using FiberHelp.Models;
using FiberHelp.Helpers;
using System;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FiberHelp.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {
            try
            {
                context.Database.EnsureCreated();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DbInitializer: EnsureCreated error: {ex.Message}");
            }

            // Ensure new lockout columns exist on Agents and Technicians tables
            EnsureLockoutColumns(context);
            EnsurePasswordResetTable(context);

            // Read admin credentials from environment variables (secure coding practice)
            var adminEmail = Environment.GetEnvironmentVariable("FIBERHELP_ADMIN_EMAIL");
            var adminPassword = Environment.GetEnvironmentVariable("FIBERHELP_ADMIN_PASSWORD");

            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            {
                System.Diagnostics.Debug.WriteLine("DbInitializer: FIBERHELP_ADMIN_EMAIL/FIBERHELP_ADMIN_PASSWORD not set. Skipping admin seed.");
                return;
            }

            if (!InputValidator.IsValidEmail(adminEmail))
            {
                System.Diagnostics.Debug.WriteLine("DbInitializer: FIBERHELP_ADMIN_EMAIL is invalid. Skipping admin seed.");
                return;
            }

            var adminHash = PasswordHasher.Hash(adminPassword);

            // Users table seeding (ONLY Administrator - agents/technicians should be created manually)
            try
            {
                var admin = context.Users.FirstOrDefault(u => u.Email == adminEmail);
                if (admin == null)
                {
                    admin = new User { Email = adminEmail, PasswordHash = adminHash, Role = "Administrator", FullName = "System Admin" };
                    context.Users.Add(admin);
                    System.Diagnostics.Debug.WriteLine("DbInitializer: Created admin in Users table");
                }
                else if (PasswordHasher.NeedsUpgrade(admin.PasswordHash))
                {
                    // Upgrade legacy SHA256 hash to PBKDF2
                    admin.PasswordHash = adminHash;
                    admin.Role = "Administrator";
                    context.Users.Update(admin);
                    System.Diagnostics.Debug.WriteLine("DbInitializer: Upgraded admin password hash to PBKDF2");
                }

                context.SaveChanges();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DbInitializer: Users table error: {ex.Message}");
            }

            System.Diagnostics.Debug.WriteLine("DbInitializer: Initialization complete");
        }

        /// <summary>
        /// Public entry point for ensuring lockout columns exist (used by AuthService for cloud DB).
        /// </summary>
        public static void EnsureLockoutColumnsPublic(AppDbContext context) => EnsureLockoutColumns(context);

        private static void EnsurePasswordResetTable(AppDbContext context)
        {
            try
            {
                bool isSqlite = context.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;

                if (isSqlite)
                {
                    context.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS PasswordResetTokens (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Email TEXT NOT NULL,
    AccountType TEXT NOT NULL,
    CodeHash TEXT NOT NULL,
    ExpiresAt TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    UsedAt TEXT NULL,
    AttemptCount INTEGER NOT NULL DEFAULT 0
);");
                    context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_PasswordResetTokens_Email ON PasswordResetTokens(Email);");
                }
                else
                {
                    context.Database.ExecuteSqlRaw(@"
IF OBJECT_ID(N'[dbo].[PasswordResetTokens]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PasswordResetTokens](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Email] NVARCHAR(256) NOT NULL,
        [AccountType] NVARCHAR(30) NOT NULL,
        [CodeHash] NVARCHAR(200) NOT NULL,
        [ExpiresAt] DATETIME2 NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [UsedAt] DATETIME2 NULL,
        [AttemptCount] INT NOT NULL CONSTRAINT [DF_PasswordResetTokens_AttemptCount] DEFAULT 0
    );
END;");
                    context.Database.ExecuteSqlRaw(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PasswordResetTokens_Email' AND object_id = OBJECT_ID('[dbo].[PasswordResetTokens]'))
BEGIN
    CREATE INDEX [IX_PasswordResetTokens_Email] ON [dbo].[PasswordResetTokens]([Email]);
END;");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DbInitializer: EnsurePasswordResetTable error: {ex.Message}");
            }
        }

        private static void EnsureLockoutColumns(AppDbContext context)
        {
            var tables = new[] { "Agents", "Technicians" };
            var columns = new (string Name, string SqliteType, string SqlServerType)[] 
            { 
                ("LockedUntil", "TEXT", "DATETIME2 NULL"),
                ("LockoutCount", "INTEGER DEFAULT 0", "INT NOT NULL DEFAULT 0")
            };

            bool isSqlite = context.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;

            foreach (var table in tables)
            {
                foreach (var col in columns)
                {
                    try
                    {
                        bool columnExists = false;
                        // When data masking is applied, real tables are renamed to {table}_Data
                        string actualTable = table;
                        if (!isSqlite)
                        {
                            var conn = context.Database.GetDbConnection();
                            if (conn.State != System.Data.ConnectionState.Open) conn.Open();
                            using var chk = conn.CreateCommand();
                            chk.CommandText = $"SELECT OBJECT_ID('dbo.{table}_Data', 'U')";
                            var chkResult = chk.ExecuteScalar();
                            if (chkResult != null && chkResult != DBNull.Value)
                                actualTable = table + "_Data";
                        }

                        if (isSqlite)
                        {
                            // SQLite: use PRAGMA
                            var conn = context.Database.GetDbConnection();
                            if (conn.State != System.Data.ConnectionState.Open) conn.Open();
                            using var cmd = conn.CreateCommand();
                            cmd.CommandText = $"PRAGMA table_info({actualTable})";
                            using var reader = cmd.ExecuteReader();
                            while (reader.Read())
                            {
                                if (string.Equals(reader.GetString(1), col.Name, StringComparison.OrdinalIgnoreCase))
                                {
                                    columnExists = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            // SQL Server: check INFORMATION_SCHEMA
                            var conn = context.Database.GetDbConnection();
                            if (conn.State != System.Data.ConnectionState.Open) conn.Open();
                            using var cmd = conn.CreateCommand();
                            cmd.CommandText = $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{actualTable}' AND COLUMN_NAME = '{col.Name}'";
                            var result = cmd.ExecuteScalar();
                            columnExists = Convert.ToInt32(result) > 0;
                        }

                        if (!columnExists)
                        {
                            var colType = isSqlite ? col.SqliteType : col.SqlServerType;
                            context.Database.ExecuteSqlRaw($"ALTER TABLE {(isSqlite ? actualTable : $"[dbo].[{actualTable}]")} ADD [{col.Name}] {colType}");
                            System.Diagnostics.Debug.WriteLine($"DbInitializer: Added {col.Name} column to {actualTable}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"DbInitializer: Error adding {col.Name} to {table}: {ex.Message}");
                    }
                }
            }
        }
    }
}
