using Microsoft.Extensions.Logging;
using FiberHelp.Services;
using Microsoft.EntityFrameworkCore;
using FiberHelp.Data.context;
using Microsoft.Extensions.DependencyInjection;
using FiberHelp.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Storage;
using System.IO;

namespace FiberHelp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            // Load configuration from appsettings.json (copied to output via csproj)
            builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            // Note: environment variables are loaded into the process by LoadDotEnvIfPresent
            // and can be read via Configuration or Environment.GetEnvironmentVariable.

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddSingleton<AuditLoggingService>();
            builder.Services.AddSingleton<EmailOtpService>();
            builder.Services.AddSingleton<AuthService>();
            builder.Services.AddSingleton<ErrorHandlingService>();
            builder.Services.AddScoped<AdminService>();
            builder.Services.AddScoped<BillingService>();
            builder.Services.AddScoped<TransactionService>();
            builder.Services.AddSingleton<DataSyncService>();
            builder.Services.AddSingleton<DualWriteService>();
            builder.Services.AddSingleton<OutboxProcessor>();
            builder.Services.AddSingleton<SyncScheduler>(sp => new SyncScheduler(sp.GetRequiredService<IServiceScopeFactory>(), TimeSpan.FromMinutes(5)));

            // NOTE: AddControllers() and MapControllers() are removed because MAUI is a client app, not an API server.

            var localDbPath = Path.Combine(FileSystem.AppDataDirectory, "fiberhelp_local.db");
            var sqliteConn = $"Data Source={localDbPath}";

            // Load .env values (if present) into process environment for local development.
            LoadDotEnvIfPresent();

            // Prefer environment variables for sensitive connection strings (secure coding practice)
            // Keep a non-secret LocalDB fallback for development if env var is not supplied.
            var sqlLocal = Environment.GetEnvironmentVariable("FIBERHELP_LOCAL_CONNECTION");
            if (string.IsNullOrWhiteSpace(sqlLocal))
            {
                sqlLocal = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=FiberhelpDB;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";
                System.Diagnostics.Debug.WriteLine("MauiProgram: FIBERHELP_LOCAL_CONNECTION not set. Using secure LocalDB fallback.");
            }

            var sqlOnline = Environment.GetEnvironmentVariable("FIBERHELP_ONLINE_CONNECTION") ?? string.Empty;

            // Register contexts: SQLite (offline), SqlServer (online), SqlServer local dev (MSSQLLocalDB)
            builder.Services.AddDbContext<localContext>(o => o.UseSqlite(sqliteConn));
            builder.Services.AddDbContext<onlineContext>(o =>
            {
                if (!string.IsNullOrWhiteSpace(sqlOnline))
                    o.UseSqlServer(sqlOnline, sql => sql.EnableRetryOnFailure());
                else
                    o.UseSqlite(sqliteConn); // Fallback to SQLite so resolving onlineContext never throws
            });
            builder.Services.AddDbContext<devLocalContext>(o => o.UseSqlServer(sqlLocal));

            // DEFAULT to SQL Server LocalDB for AppDbContext so login uses your FiberhelpDB
            builder.Services.AddScoped<AppDbContext>(sp => sp.GetRequiredService<devLocalContext>());

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            // Initialize SQL Server LocalDB (FiberhelpDB) and seed if needed
            using (var scope = app.Services.CreateScope())
            {
                try
                {
                    var devLocal = scope.ServiceProvider.GetRequiredService<devLocalContext>();

                    // Safely ensure ClientFeedbacks table exists without deleting other data
                    try
                    {
                        var conn = devLocal.Database.GetDbConnection();
                        if (conn.State != System.Data.ConnectionState.Open) conn.Open();
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = @"
                            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ClientFeedbacks')
                            BEGIN
                                CREATE TABLE [ClientFeedbacks] (
                                    [Id] int NOT NULL IDENTITY,
                                    [TransactionProofId] int NULL,
                                    [TicketId] int NULL,
                                    [InvoiceId] int NULL,
                                    [ClientId] nvarchar(50) NOT NULL,
                                    [ClientName] nvarchar(200) NULL,
                                    [Rating] int NOT NULL,
                                    [ServiceQualityRating] int NULL,
                                    [ResponseTimeRating] int NULL,
                                    [ProfessionalismRating] int NULL,
                                    [CommunicationRating] int NULL,
                                    [WouldRecommend] bit NULL,
                                    [Comments] nvarchar(2000) NULL,
                                    [FeedbackCategory] nvarchar(50) NOT NULL,
                                    [HandledById] nvarchar(50) NULL,
                                    [HandledByName] nvarchar(200) NULL,
                                    [SubmittedAt] datetime2 NOT NULL,
                                    [IsReviewed] bit NOT NULL,
                                    [ReviewedAt] datetime2 NULL,
                                    [ReviewedById] nvarchar(50) NULL,
                                    [ReviewNotes] nvarchar(1000) NULL,
                                    [Status] nvarchar(50) NOT NULL,
                                    [RequiresFollowUp] bit NOT NULL,
                                    [FollowUpNotes] nvarchar(1000) NULL,
                                    [FollowUpCompletedAt] datetime2 NULL,
                                    CONSTRAINT [PK_ClientFeedbacks] PRIMARY KEY ([Id])
                                );
                            END";
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Safe table creation failed: {ex.Message}"); }

                    DbInitializer.Initialize(devLocal);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Dev Local DB init error: {ex.Message}");
                }

                // Keep SQLite ready for offline fallback (optional)
                try
                {
                    var local = scope.ServiceProvider.GetRequiredService<localContext>();
                    local.Database.EnsureCreated();
                }
                catch { }
            }

            // Ensure local (SQLite) Outbox table exists for offline sync
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = app.Services.CreateScope();
                    var local = scope.ServiceProvider.GetRequiredService<localContext>();
                    await DualWriteService.EnsureOutboxTableAsync(local);
                }
                catch { }
            });

            // Start periodic cloud -> local sync
            _ = Task.Run(() =>
            {
                var scheduler = app.Services.GetRequiredService<SyncScheduler>();
                scheduler.Start();
            });

            // Start outbox processor to push local offline writes to cloud when online
            _ = Task.Run(() =>
            {
                var proc = app.Services.GetRequiredService<OutboxProcessor>();
                proc.Start();
            });
            
            System.Diagnostics.Debug.WriteLine("MauiProgram: Background sync tasks re-enabled (5 min interval).");

            return app;
        }

        private static void LoadDotEnvIfPresent()
        {
            try
            {
                var envPath = FindDotEnvPath();
                if (string.IsNullOrWhiteSpace(envPath) || !File.Exists(envPath))
                    return;

                foreach (var rawLine in File.ReadLines(envPath))
                {
                    var line = rawLine?.Trim();
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    var separatorIndex = line.IndexOf('=');
                    if (separatorIndex <= 0)
                        continue;

                    var key = line[..separatorIndex].Trim();
                    var value = line[(separatorIndex + 1)..].Trim();

                    if (string.IsNullOrWhiteSpace(key))
                        continue;

                    if (value.Length >= 2 &&
                        ((value.StartsWith('"') && value.EndsWith('"')) ||
                         (value.StartsWith('\'') && value.EndsWith('\''))))
                    {
                        value = value[1..^1];
                    }

                    // Respect OS-level env vars if already set.
                    if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
                    {
                        Environment.SetEnvironmentVariable(key, value);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"MauiProgram: Loaded .env from {envPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MauiProgram: Failed to load .env: {ex.Message}");
            }
        }

        private static string? FindDotEnvPath()
        {
            var startDirs = new[]
            {
                Directory.GetCurrentDirectory(),
                AppContext.BaseDirectory
            };

            foreach (var startDir in startDirs)
            {
                if (string.IsNullOrWhiteSpace(startDir) || !Directory.Exists(startDir))
                    continue;

                var current = new DirectoryInfo(startDir);
                var depth = 0;
                while (current != null && depth < 8)
                {
                    var candidate = Path.Combine(current.FullName, ".env");
                    if (File.Exists(candidate))
                        return candidate;

                    current = current.Parent;
                    depth++;
                }
            }

            return null;
        }
    }
}
