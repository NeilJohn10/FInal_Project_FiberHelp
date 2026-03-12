using System;
using System.Threading.Tasks;
using FiberHelp.Data;
using FiberHelp.Data.context;
using FiberHelp.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using FiberHelp.Models;

namespace FiberHelp.Services
{
    public class AuthService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly AuditLoggingService _auditLog;
        private readonly EmailOtpService _emailOtpService;
        public AuthService(IServiceProvider serviceProvider, AuditLoggingService auditLog, EmailOtpService emailOtpService) 
        { 
            _serviceProvider = serviceProvider;
            _auditLog = auditLog;
            _emailOtpService = emailOtpService;
        }

        public bool IsAuthenticated { get; private set; }
        public string? Email { get; private set; }
        public string? Role { get; private set; }
        public string? FullName { get; private set; }
        public string? UserId { get; private set; }
        public string? ServiceArea { get; private set; }
        public string? LastLoginError { get; private set; }
        public DateTime? LockoutExpiresAt { get; private set; }
        public event Action? AuthenticationStateChanged;

        private const int ResetCodeExpiryMinutes = 10;
        private const int ResetMaxAttempts = 5;
        private const int ResetRequestCooldownSeconds = 60;
        private const int ResetRequestWindowMinutes = 15;
        private const int ResetRequestMaxPerWindow = 3;

        public async Task<bool> SignInAsync(string email, string password)
        {
            LastLoginError = null;
            LockoutExpiresAt = null;

            try
            {
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    LastLoginError = "Email and password are required.";
                    return false;
                }

                using var scope = _serviceProvider.CreateScope();
                var _db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                try 
                { 
                    await _db.Database.EnsureCreatedAsync(); 
                    DbInitializer.Initialize(_db); 
                } 
                catch (Exception ex) 
                { 
                    System.Diagnostics.Debug.WriteLine($"AuthService: DB init error: {ex.Message}");
                }

                var lowered = email.Trim().ToLowerInvariant();

                System.Diagnostics.Debug.WriteLine($"AuthService: Attempting login for '{lowered}'");

                // Try local database first, then cloud if available
                var result = await TryLoginFromContext(_db, lowered, password);
                if (result) return true;

                // Only try cloud if the user was NOT found locally (i.e., no error was set yet).
                // If LastLoginError is already set, the user was found but auth failed — don't retry on cloud.
                if (string.IsNullOrEmpty(LastLoginError))
                {
                    try
                    {
                        var onlineContext = scope.ServiceProvider.GetService<onlineContext>();
                        if (onlineContext != null)
                        {
                            try
                            {
                                if (onlineContext.Database.CanConnect())
                                {
                                    // Ensure lockout columns exist on cloud DB before querying
                                    try { DbInitializer.EnsureLockoutColumnsPublic(onlineContext); }
                                    catch (Exception colEx) { System.Diagnostics.Debug.WriteLine($"AuthService: Cloud column check: {colEx.Message}"); }

                                    System.Diagnostics.Debug.WriteLine($"AuthService: Local login failed, trying cloud database...");
                                    result = await TryLoginFromContext(onlineContext, lowered, password);
                                    if (result) return true;
                                }
                            }
                            catch (Exception cloudEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"AuthService: Cloud login check failed: {cloudEx.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"AuthService: Cloud context error: {ex.Message}");
                    }
                }

                // Only log here if no failure was already logged inside TryLoginFromContext
                if (string.IsNullOrEmpty(LastLoginError))
                {
                    LastLoginError = "Email not found.";
                    System.Diagnostics.Debug.WriteLine($"AuthService: No user found with email '{lowered}'");
                    await _auditLog.LogLoginFailureAsync(lowered, "Email not found");
                }
                return false;
            }
            catch (Exception ex)
            {
                LastLoginError = "An unexpected error occurred during login. Please try again.";
                System.Diagnostics.Debug.WriteLine($"AuthService: Exception during login: {ex}");
                await _auditLog.LogErrorAsync(ex, operation: "Login", entityType: "Authentication");
                return false;
            }
        }

        private async Task<bool> TryLoginFromContext(AppDbContext db, string loweredEmail, string plainPassword)
        {
            // 1. Check Users table first (Administrator, CSR)
            try
            {
                var user = await db.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == loweredEmail);

                if (user != null)
                {
                    System.Diagnostics.Debug.WriteLine($"AuthService: Found user in Users table: {user.Email}, Role: {user.Role}");

                    if (!user.IsActive)
                    {
                        LastLoginError = "Account is inactive. Contact administrator.";
                        await _auditLog.LogLoginFailureAsync(loweredEmail, "User account inactive");
                        return false;
                    }

                    if (!PasswordHasher.Verify(plainPassword, user.PasswordHash))
                    {
                        LastLoginError = "Invalid password.";
                        await _auditLog.LogLoginFailureAsync(loweredEmail, "Invalid password");
                        return false;
                    }

                    // Auto-upgrade legacy SHA256 hash to PBKDF2
                    if (PasswordHasher.NeedsUpgrade(user.PasswordHash))
                    {
                        try
                        {
                            var tracked = await db.Users.FindAsync(user.Id);
                            if (tracked != null)
                            {
                                tracked.PasswordHash = PasswordHasher.Hash(plainPassword);
                                await db.SaveChangesAsync();
                                System.Diagnostics.Debug.WriteLine("AuthService: Upgraded user password hash to PBKDF2");
                            }
                        }
                        catch (Exception upgradeEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"AuthService: Hash upgrade failed: {upgradeEx.Message}");
                        }
                    }

                    IsAuthenticated = true;
                    Email = user.Email;
                    Role = user.Role;
                    FullName = user.FullName;
                    UserId = user.Id;
                    ServiceArea = null;

                    System.Diagnostics.Debug.WriteLine($"AuthService: User login successful! Role = {Role}");
                    await _auditLog.LogLoginSuccessAsync(user.Id, user.Email, user.Role);
                    AuthenticationStateChanged?.Invoke();
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AuthService: Users table check error: {ex.Message}");
                await _auditLog.LogErrorAsync(ex, operation: "Login", entityType: "User");
            }

            // 2. Check Agents table (Support Agent, Supervisor)
            try
            {
                Agent? agent = null;
                try
                {
                    agent = await db.Agents.AsNoTracking()
                        .FirstOrDefaultAsync(a => a.Email.ToLower() == loweredEmail);
                }
                catch (Exception queryEx)
                {
                    System.Diagnostics.Debug.WriteLine($"AuthService: Agent query with archive fields failed, trying without: {queryEx.Message}");
                    try
                    {
                        var agents = await db.Agents.AsNoTracking().ToListAsync();
                        agent = agents.FirstOrDefault(a => a.Email?.ToLower() == loweredEmail);
                    }
                    catch (Exception fallbackEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"AuthService: Agent fallback query also failed: {fallbackEx.Message}");
                    }
                }

                if (agent != null)
                {
                    System.Diagnostics.Debug.WriteLine($"AuthService: Found agent '{agent.Email}', IsActive={agent.IsActive}, IsLocked={agent.IsLocked}");

                    if (agent.IsArchived)
                    {
                        LastLoginError = "Account has been archived. Contact administrator.";
                        await _auditLog.LogLoginFailureAsync(loweredEmail, "Account archived");
                        return false;
                    }

                    if (!agent.IsActive)
                    {
                        LastLoginError = "Account is inactive. Contact administrator.";
                        await _auditLog.LogLoginFailureAsync(loweredEmail, "Account inactive");
                        return false;
                    }

                    // Check time-based lockout
                    if (agent.IsLocked)
                    {
                        if (agent.LockedUntil.HasValue && DateTime.UtcNow < agent.LockedUntil.Value)
                        {
                            var remaining = agent.LockedUntil.Value - DateTime.UtcNow;
                            var minsLeft = (int)Math.Ceiling(remaining.TotalMinutes);
                            LockoutExpiresAt = agent.LockedUntil.Value;
                            LastLoginError = $"Account is suspended. Try again in {minsLeft} minute(s).";
                            await _auditLog.LogLoginFailureAsync(loweredEmail, $"Account suspended ({minsLeft} min remaining)");
                            return false;
                        }
                        else
                        {
                            // Lockout expired — unlock and reset failed count
                            try
                            {
                                var tracked = await db.Agents.FirstOrDefaultAsync(a => a.Id == agent.Id);
                                if (tracked != null)
                                {
                                    tracked.IsLocked = false;
                                    tracked.FailedLoginCount = 0;
                                    tracked.LockedUntil = null;
                                    await db.SaveChangesAsync();
                                    agent.IsLocked = false;
                                    agent.FailedLoginCount = 0;
                                }
                            }
                            catch { }
                        }
                    }

                    if (!PasswordHasher.Verify(plainPassword, agent.PasswordHash))
                    {
                        try
                        {
                            var tracked = await db.Agents.FirstOrDefaultAsync(a => a.Id == agent.Id);
                            if (tracked != null)
                            {
                                tracked.FailedLoginCount++;
                                if (tracked.FailedLoginCount >= 5)
                                {
                                    tracked.IsLocked = true;
                                    tracked.LockoutCount++;
                                    // Progressive suspension: 15 min first time, 30 min after
                                    var suspendMinutes = tracked.LockoutCount <= 1 ? 15 : 30;
                                    tracked.LockedUntil = DateTime.UtcNow.AddMinutes(suspendMinutes);
                                    tracked.FailedLoginCount = 0;
                                    LockoutExpiresAt = tracked.LockedUntil;
                                    LastLoginError = $"Too many failed attempts. Account suspended for {suspendMinutes} minutes.";
                                    await db.SaveChangesAsync();
                                    await _auditLog.LogLoginFailureAsync(loweredEmail, $"Account suspended for {suspendMinutes} minutes");
                                    return false;
                                }
                                await db.SaveChangesAsync();
                            }
                        }
                        catch { }
                        var attemptsLeft = 5 - ((agent.FailedLoginCount) + 1);
                        if (attemptsLeft < 0) attemptsLeft = 0;
                        LastLoginError = $"Invalid password. {(attemptsLeft > 0 ? $"{attemptsLeft} attempt(s) remaining." : "")}";
                        await _auditLog.LogLoginFailureAsync(loweredEmail, "Invalid password");
                        return false;
                    }

                    // Auto-upgrade legacy hash to PBKDF2
                    try
                    {
                        var trackedSuccess = await db.Agents.FirstOrDefaultAsync(a => a.Id == agent.Id);
                        if (trackedSuccess != null)
                        {
                            trackedSuccess.FailedLoginCount = 0;
                            trackedSuccess.LockoutCount = 0;
                            trackedSuccess.LockedUntil = null;
                            trackedSuccess.LastLoginAt = DateTime.UtcNow;
                            if (PasswordHasher.NeedsUpgrade(agent.PasswordHash))
                            {
                                trackedSuccess.PasswordHash = PasswordHasher.Hash(plainPassword);
                                System.Diagnostics.Debug.WriteLine("AuthService: Upgraded agent password hash to PBKDF2");
                            }
                            await db.SaveChangesAsync();
                        }
                    }
                    catch { }

                    IsAuthenticated = true;
                    Email = agent.Email;
                    UserId = agent.Id;
                    FullName = agent.FullName;
                    Role = string.IsNullOrWhiteSpace(agent.Role) ? "Agent" : agent.Role;
                    ServiceArea = agent.ServiceArea;

                    System.Diagnostics.Debug.WriteLine($"AuthService: Agent login successful! Role = {Role}");
                    await _auditLog.LogLoginSuccessAsync(agent.Id, agent.Email, Role);
                    AuthenticationStateChanged?.Invoke();
                    return true;
                }
            }
            catch (Exception agentEx)
            {
                System.Diagnostics.Debug.WriteLine($"AuthService: Agents table error: {agentEx.Message}");
            }

            // 3. Check Technicians table (dedicated technicians)
            try
            {
                Technician? technician = null;
                try
                {
                    technician = await db.Technicians.AsNoTracking()
                        .FirstOrDefaultAsync(t => t.Email.ToLower() == loweredEmail);
                }
                catch (Exception queryEx)
                {
                    System.Diagnostics.Debug.WriteLine($"AuthService: Technician query with archive fields failed, trying without: {queryEx.Message}");
                    try
                    {
                        var techs = await db.Technicians.AsNoTracking().ToListAsync();
                        technician = techs.FirstOrDefault(t => t.Email?.ToLower() == loweredEmail);
                    }
                    catch (Exception fallbackEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"AuthService: Technician fallback query also failed: {fallbackEx.Message}");
                    }
                }

                if (technician != null)
                {
                    System.Diagnostics.Debug.WriteLine($"AuthService: Found technician '{technician.Email}', IsActive={technician.IsActive}, IsLocked={technician.IsLocked}");

                    if (technician.IsArchived)
                    {
                        LastLoginError = "Account has been archived. Contact administrator.";
                        await _auditLog.LogLoginFailureAsync(loweredEmail, "Account archived");
                        return false;
                    }

                    if (!technician.IsActive)
                    {
                        LastLoginError = "Account is inactive. Contact administrator.";
                        await _auditLog.LogLoginFailureAsync(loweredEmail, "Account inactive");
                        return false;
                    }

                    // Check time-based lockout
                    if (technician.IsLocked)
                    {
                        if (technician.LockedUntil.HasValue && DateTime.UtcNow < technician.LockedUntil.Value)
                        {
                            var remaining = technician.LockedUntil.Value - DateTime.UtcNow;
                            var minsLeft = (int)Math.Ceiling(remaining.TotalMinutes);
                            LockoutExpiresAt = technician.LockedUntil.Value;
                            LastLoginError = $"Account is suspended. Try again in {minsLeft} minute(s).";
                            await _auditLog.LogLoginFailureAsync(loweredEmail, $"Account suspended ({minsLeft} min remaining)");
                            return false;
                        }
                        else
                        {
                            // Lockout expired — unlock and reset failed count
                            try
                            {
                                var tracked = await db.Technicians.FirstOrDefaultAsync(t => t.Id == technician.Id);
                                if (tracked != null)
                                {
                                    tracked.IsLocked = false;
                                    tracked.FailedLoginCount = 0;
                                    tracked.LockedUntil = null;
                                    await db.SaveChangesAsync();
                                    technician.IsLocked = false;
                                    technician.FailedLoginCount = 0;
                                }
                            }
                            catch { }
                        }
                    }

                    if (!PasswordHasher.Verify(plainPassword, technician.PasswordHash))
                    {
                        try
                        {
                            var tracked = await db.Technicians.FirstOrDefaultAsync(t => t.Id == technician.Id);
                            if (tracked != null)
                            {
                                tracked.FailedLoginCount++;
                                if (tracked.FailedLoginCount >= 5)
                                {
                                    tracked.IsLocked = true;
                                    tracked.LockoutCount++;
                                    var suspendMinutes = tracked.LockoutCount <= 1 ? 15 : 30;
                                    tracked.LockedUntil = DateTime.UtcNow.AddMinutes(suspendMinutes);
                                    tracked.FailedLoginCount = 0;
                                    LockoutExpiresAt = tracked.LockedUntil;
                                    LastLoginError = $"Too many failed attempts. Account suspended for {suspendMinutes} minutes.";
                                    await db.SaveChangesAsync();
                                    await _auditLog.LogLoginFailureAsync(loweredEmail, $"Account suspended for {suspendMinutes} minutes");
                                    return false;
                                }
                                await db.SaveChangesAsync();
                            }
                        }
                        catch { }
                        var techAttemptsLeft = 5 - ((technician.FailedLoginCount) + 1);
                        if (techAttemptsLeft < 0) techAttemptsLeft = 0;
                        LastLoginError = $"Invalid password. {(techAttemptsLeft > 0 ? $"{techAttemptsLeft} attempt(s) remaining." : "")}";
                        await _auditLog.LogLoginFailureAsync(loweredEmail, "Invalid password");
                        return false;
                    }

                    // Auto-upgrade legacy hash to PBKDF2
                    try
                    {
                        var trackedSuccess = await db.Technicians.FirstOrDefaultAsync(t => t.Id == technician.Id);
                        if (trackedSuccess != null)
                        {
                            trackedSuccess.FailedLoginCount = 0;
                            trackedSuccess.LockoutCount = 0;
                            trackedSuccess.LockedUntil = null;
                            trackedSuccess.LastLoginAt = DateTime.UtcNow;
                            if (PasswordHasher.NeedsUpgrade(technician.PasswordHash))
                            {
                                trackedSuccess.PasswordHash = PasswordHasher.Hash(plainPassword);
                                System.Diagnostics.Debug.WriteLine("AuthService: Upgraded technician password hash to PBKDF2");
                            }
                            await db.SaveChangesAsync();
                        }
                    }
                    catch { }

                    IsAuthenticated = true;
                    Email = technician.Email;
                    UserId = technician.Id;
                    FullName = technician.FullName;
                    Role = "Technician";
                    ServiceArea = technician.ServiceArea;

                    System.Diagnostics.Debug.WriteLine($"AuthService: Technician login successful!");
                    await _auditLog.LogLoginSuccessAsync(technician.Id, technician.Email, "Technician");
                    AuthenticationStateChanged?.Invoke();
                    return true;
                }
            }
            catch (Exception techEx)
            {
                System.Diagnostics.Debug.WriteLine($"AuthService: Technicians table error: {techEx.Message}");
            }

            return false;
        }

        public async Task<(bool Success, string Message)> RequestPasswordResetAsync(string email)
        {
            try
            {
                if (!InputValidator.IsValidEmail(email))
                    return (false, "Please enter a valid email address.");

                var loweredEmail = email.Trim().ToLowerInvariant();
                const string genericMessage = "If this account exists, a reset code has been sent.";

                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                DbInitializer.Initialize(db);

                // Cleanup old/expired reset tokens to reduce risk surface and table bloat
                var pruneBefore = DateTime.UtcNow.AddDays(-2);
                var staleTokens = await db.PasswordResetTokens
                    .Where(t => t.ExpiresAt < DateTime.UtcNow || (t.UsedAt != null && t.CreatedAt < pruneBefore))
                    .ToListAsync();
                if (staleTokens.Count > 0)
                {
                    db.PasswordResetTokens.RemoveRange(staleTokens);
                    await db.SaveChangesAsync();
                }

                var account = await FindResetAccountAsync(db, loweredEmail);
                if (!account.Exists)
                {
                    await _auditLog.LogLoginFailureAsync(loweredEmail, "Forgot password requested for unknown email");
                    return (true, genericMessage);
                }

                var windowStart = DateTime.UtcNow.AddMinutes(-ResetRequestWindowMinutes);
                var requestsInWindow = await db.PasswordResetTokens
                    .CountAsync(t => t.Email == loweredEmail && t.CreatedAt >= windowStart);
                if (requestsInWindow >= ResetRequestMaxPerWindow)
                {
                    await _auditLog.LogLoginFailureAsync(loweredEmail, "Forgot password rate limit reached");
                    return (true, genericMessage);
                }

                var recentPending = await db.PasswordResetTokens
                    .Where(t => t.Email == loweredEmail && t.UsedAt == null && t.CreatedAt > DateTime.UtcNow.AddSeconds(-ResetRequestCooldownSeconds))
                    .OrderByDescending(t => t.CreatedAt)
                    .FirstOrDefaultAsync();

                if (recentPending != null)
                    return (true, genericMessage);

                var activeTokens = await db.PasswordResetTokens
                    .Where(t => t.Email == loweredEmail && t.UsedAt == null)
                    .ToListAsync();
                foreach (var existing in activeTokens)
                {
                    existing.UsedAt = DateTime.UtcNow;
                }

                var plainCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
                var token = new PasswordResetToken
                {
                    Email = loweredEmail,
                    AccountType = account.AccountType,
                    CodeHash = HashResetCode(plainCode),
                    ExpiresAt = DateTime.UtcNow.AddMinutes(ResetCodeExpiryMinutes),
                    CreatedAt = DateTime.UtcNow,
                    AttemptCount = 0
                };

                db.PasswordResetTokens.Add(token);
                await db.SaveChangesAsync();

                var sent = await _emailOtpService.SendPasswordResetOtpAsync(loweredEmail, plainCode, ResetCodeExpiryMinutes);

#if DEBUG
                if (!sent)
                {
                    System.Diagnostics.Debug.WriteLine($"ForgotPassword OTP fallback [{account.AccountType}] {loweredEmail}: {plainCode}");
                }
#endif

                if (!sent)
                {
                    await _auditLog.LogLoginFailureAsync(loweredEmail, "OTP email send failed");
#if DEBUG
                    return (false, "OTP email failed to send. Check Debug Output and SMTP settings.");
#else
                    token.UsedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                    return (false, "Unable to send reset code right now. Please try again later.");
#endif
                }

                await _auditLog.LogActivityAsync(account.Id ?? string.Empty, loweredEmail, account.AccountType, "RequestPasswordReset", "PasswordResetTokens");

                return (true, genericMessage);
            }
            catch (Exception ex)
            {
                await _auditLog.LogErrorAsync(ex, operation: "RequestPasswordReset", entityType: "Authentication");
                return (false, "Failed to request password reset. Please try again.");
            }
        }

        public async Task<(bool Success, string Message)> ResetPasswordAsync(string email, string code, string newPassword)
        {
            try
            {
                if (!InputValidator.IsValidEmail(email))
                    return (false, "Please enter a valid email address.");
                if (string.IsNullOrWhiteSpace(code) || code.Trim().Length != 6)
                    return (false, "Please enter the 6-digit reset code.");
                var passwordError = InputValidator.ValidatePassword(newPassword);
                if (!string.IsNullOrWhiteSpace(passwordError))
                    return (false, passwordError);

                var loweredEmail = email.Trim().ToLowerInvariant();
                var cleanCode = code.Trim();

                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                DbInitializer.Initialize(db);

                var token = await db.PasswordResetTokens
                    .Where(t => t.Email == loweredEmail && t.UsedAt == null)
                    .OrderByDescending(t => t.CreatedAt)
                    .FirstOrDefaultAsync();

                if (token == null || token.ExpiresAt <= DateTime.UtcNow)
                {
                    await _auditLog.LogLoginFailureAsync(loweredEmail, "Reset password attempted with expired/invalid token");
                    return (false, "Reset code is invalid or expired.");
                }
                if (token.AttemptCount >= ResetMaxAttempts)
                {
                    await _auditLog.LogLoginFailureAsync(loweredEmail, "Reset password max OTP attempts reached");
                    return (false, "Too many invalid attempts. Request a new reset code.");
                }

                if (!FixedTimeEqualsHex(token.CodeHash, HashResetCode(cleanCode)))
                {
                    token.AttemptCount++;
                    await db.SaveChangesAsync();
                    await _auditLog.LogLoginFailureAsync(loweredEmail, "Invalid password reset OTP");
                    return (false, "Invalid reset code.");
                }

                var updated = false;
                if (token.AccountType == "User")
                {
                    var user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == loweredEmail && u.IsActive);
                    if (user != null)
                    {
                        user.PasswordHash = PasswordHasher.Hash(newPassword);
                        updated = true;
                    }
                }
                else if (token.AccountType == "Agent")
                {
                    var agent = await db.Agents.FirstOrDefaultAsync(a => a.Email.ToLower() == loweredEmail && a.IsActive && !a.IsArchived);
                    if (agent != null)
                    {
                        agent.PasswordHash = PasswordHasher.Hash(newPassword);
                        agent.PasswordChangedAt = DateTime.UtcNow;
                        agent.FailedLoginCount = 0;
                        agent.LockoutCount = 0;
                        agent.IsLocked = false;
                        agent.LockedUntil = null;
                        updated = true;
                    }
                }
                else if (token.AccountType == "Technician")
                {
                    var tech = await db.Technicians.FirstOrDefaultAsync(t => t.Email.ToLower() == loweredEmail && t.IsActive && !t.IsArchived);
                    if (tech != null)
                    {
                        tech.PasswordHash = PasswordHasher.Hash(newPassword);
                        tech.PasswordChangedAt = DateTime.UtcNow;
                        tech.FailedLoginCount = 0;
                        tech.LockoutCount = 0;
                        tech.IsLocked = false;
                        tech.LockedUntil = null;
                        updated = true;
                    }
                }

                if (!updated)
                {
                    await _auditLog.LogLoginFailureAsync(loweredEmail, "Reset password account state invalid");
                    return (false, "Account not found for password reset.");
                }

                token.UsedAt = DateTime.UtcNow;

                var otherTokens = await db.PasswordResetTokens
                    .Where(t => t.Email == loweredEmail && t.UsedAt == null && t.Id != token.Id)
                    .ToListAsync();
                foreach (var other in otherTokens)
                {
                    other.UsedAt = DateTime.UtcNow;
                }

                await db.SaveChangesAsync();
                await _auditLog.LogActivityAsync(string.Empty, loweredEmail, token.AccountType, "ResetPassword", token.AccountType);

                return (true, "Password reset successful. You can now log in.");
            }
            catch (Exception ex)
            {
                await _auditLog.LogErrorAsync(ex, operation: "ResetPassword", entityType: "Authentication");
                return (false, "Failed to reset password. Please try again.");
            }
        }

        private static string HashResetCode(string code)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code));
            return Convert.ToHexString(bytes);
        }

        private static bool FixedTimeEqualsHex(string leftHex, string rightHex)
        {
            try
            {
                var left = Convert.FromHexString(leftHex);
                var right = Convert.FromHexString(rightHex);
                return CryptographicOperations.FixedTimeEquals(left, right);
            }
            catch
            {
                return false;
            }
        }

        private async Task<(bool Exists, string AccountType, string? Id)> FindResetAccountAsync(AppDbContext db, string loweredEmail)
        {
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email.ToLower() == loweredEmail && u.IsActive);
            if (user != null) return (true, "User", user.Id);

            var agent = await db.Agents.AsNoTracking().FirstOrDefaultAsync(a => a.Email.ToLower() == loweredEmail && !a.IsArchived && a.IsActive);
            if (agent != null) return (true, "Agent", agent.Id);

            var tech = await db.Technicians.AsNoTracking().FirstOrDefaultAsync(t => t.Email.ToLower() == loweredEmail && !t.IsArchived && t.IsActive);
            if (tech != null) return (true, "Technician", tech.Id);

            return (false, string.Empty, null);
        }

        public void SignOut()
        {
            var logEmail = Email;
            var logUserId = UserId;
            IsAuthenticated = false;
            Email = null;
            Role = null;
            FullName = null;
            UserId = null;
            ServiceArea = null;
            LastLoginError = null;
            _ = _auditLog.LogLogoutAsync(logUserId, logEmail);
            AuthenticationStateChanged?.Invoke();
        }

        // Helper methods for role checking
        public bool IsAdministrator => Role?.Equals("Administrator", StringComparison.OrdinalIgnoreCase) == true;
        public bool IsAgent => Role?.Equals("Agent", StringComparison.OrdinalIgnoreCase) == true 
                            || Role?.Equals("Support Agent", StringComparison.OrdinalIgnoreCase) == true;
        public bool IsTechnician => Role?.Equals("Technician", StringComparison.OrdinalIgnoreCase) == true;
        public bool IsSupervisor => Role?.Equals("Supervisor", StringComparison.OrdinalIgnoreCase) == true;
        public bool CanManageTickets => IsAdministrator || IsAgent || IsSupervisor;
        public bool CanResolveTickets => IsTechnician || IsAdministrator;
        public bool CanAssignTechnicians => IsAgent || IsAdministrator || IsSupervisor;
    }
}
