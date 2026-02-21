using System;
using System.Threading.Tasks;
using FiberHelp.Data;
using FiberHelp.Data.context;
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
        public AuthService(IServiceProvider serviceProvider) { _serviceProvider = serviceProvider; }

        public bool IsAuthenticated { get; private set; }
        public string? Email { get; private set; }
        public string? Role { get; private set; }
        public string? FullName { get; private set; }
        public string? UserId { get; private set; }
        public string? ServiceArea { get; private set; } // For technicians
        public string? LastLoginError { get; private set; }
        public event Action? AuthenticationStateChanged;

        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var normalized = (password ?? string.Empty).Trim();
            var bytes = Encoding.UTF8.GetBytes(normalized);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }

        public async Task<bool> SignInAsync(string email, string password)
        {
            LastLoginError = null;
            
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
                var hashed = HashPassword(password);
                
                System.Diagnostics.Debug.WriteLine($"AuthService: Attempting login for '{lowered}'");

                // Try local database first, then cloud if available
                var result = await TryLoginFromContext(_db, lowered, hashed, password);
                if (result) return true;

                // If local login fails, try cloud database
                try
                {
                    var onlineContext = scope.ServiceProvider.GetService<onlineContext>();
                    if (onlineContext != null)
                    {
                        try
                        {
                            if (onlineContext.Database.CanConnect())
                            {
                                System.Diagnostics.Debug.WriteLine($"AuthService: Local login failed, trying cloud database...");
                                result = await TryLoginFromContext(onlineContext, lowered, hashed, password);
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

                if (string.IsNullOrEmpty(LastLoginError))
                {
                    LastLoginError = "Email not found.";
                }
                System.Diagnostics.Debug.WriteLine($"AuthService: No user found with email '{lowered}'");
                return false;
            }
            catch (Exception ex)
            {
                LastLoginError = $"Login error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"AuthService: Exception during login: {ex}");
                return false;
            }
        }

        private async Task<bool> TryLoginFromContext(AppDbContext db, string loweredEmail, string hashedPassword, string plainPassword)
        {
            // 1. Check Users table first (Administrator, CSR)
            try
            {
                var user = await db.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == loweredEmail);
                    
                if (user != null)
                {
                    System.Diagnostics.Debug.WriteLine($"AuthService: Found user in Users table: {user.Email}, Role: {user.Role}");
                    
                    if (!string.Equals(user.PasswordHash?.Trim(), hashedPassword, StringComparison.Ordinal))
                    {
                        LastLoginError = "Invalid password.";
                        return false;
                    }

                    IsAuthenticated = true;
                    Email = user.Email;
                    Role = user.Role;
                    FullName = user.FullName;
                    UserId = user.Id;
                    ServiceArea = null;
                    
                    System.Diagnostics.Debug.WriteLine($"AuthService: User login successful! Role = {Role}");
                    AuthenticationStateChanged?.Invoke();
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AuthService: Users table check error: {ex.Message}");
            }

            // 2. Check Agents table (Support Agent, Supervisor)
            try
            {
                // Use raw SQL to handle cases where IsArchived column may not exist yet
                Agent? agent = null;
                try
                {
                    agent = await db.Agents.AsNoTracking()
                        .FirstOrDefaultAsync(a => a.Email.ToLower() == loweredEmail);
                }
                catch (Exception queryEx)
                {
                    System.Diagnostics.Debug.WriteLine($"AuthService: Agent query with archive fields failed, trying without: {queryEx.Message}");
                    // Fallback: query without archive fields using raw approach
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
                    
                    // Check if archived (may not be present in older databases)
                    if (agent.IsArchived)
                    {
                        LastLoginError = "Account has been archived. Contact administrator.";
                        return false;
                    }
                    
                    if (!agent.IsActive)
                    {
                        LastLoginError = "Account is inactive. Contact administrator.";
                        return false;
                    }
                    
                    if (agent.IsLocked)
                    {
                        LastLoginError = "Account is locked due to too many failed attempts.";
                        return false;
                    }
                    
                    if (!string.Equals(agent.PasswordHash?.Trim(), hashedPassword, StringComparison.Ordinal))
                    {
                        try
                        {
                            var tracked = await db.Agents.FirstOrDefaultAsync(a => a.Id == agent.Id);
                            if (tracked != null)
                            {
                                tracked.FailedLoginCount++;
                                if (tracked.FailedLoginCount >= 5) tracked.IsLocked = true;
                                await db.SaveChangesAsync();
                            }
                        }
                        catch { }
                        LastLoginError = "Invalid password.";
                        return false;
                    }

                    try
                    {
                        var trackedSuccess = await db.Agents.FirstOrDefaultAsync(a => a.Id == agent.Id);
                        if (trackedSuccess != null)
                        {
                            trackedSuccess.FailedLoginCount = 0;
                            trackedSuccess.LastLoginAt = DateTime.UtcNow;
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
                    
                    // Check if archived
                    if (technician.IsArchived)
                    {
                        LastLoginError = "Account has been archived. Contact administrator.";
                        return false;
                    }
                    
                    if (!technician.IsActive)
                    {
                        LastLoginError = "Account is inactive. Contact administrator.";
                        return false;
                    }
                    
                    if (technician.IsLocked)
                    {
                        LastLoginError = "Account is locked due to too many failed attempts.";
                        return false;
                    }
                    
                    if (!string.Equals(technician.PasswordHash?.Trim(), hashedPassword, StringComparison.Ordinal))
                    {
                        // Increment failed login count
                        try
                        {
                            var tracked = await db.Technicians.FirstOrDefaultAsync(t => t.Id == technician.Id);
                            if (tracked != null)
                            {
                                tracked.FailedLoginCount++;
                                if (tracked.FailedLoginCount >= 5) tracked.IsLocked = true;
                                await db.SaveChangesAsync();
                            }
                        }
                        catch { }
                        LastLoginError = "Invalid password.";
                        return false;
                    }

                    // Successful login - reset failed count
                    try
                    {
                        var trackedSuccess = await db.Technicians.FirstOrDefaultAsync(t => t.Id == technician.Id);
                        if (trackedSuccess != null)
                        {
                            trackedSuccess.FailedLoginCount = 0;
                            trackedSuccess.LastLoginAt = DateTime.UtcNow;
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

        public void SignOut()
        {
            IsAuthenticated = false;
            Email = null;
            Role = null;
            FullName = null;
            UserId = null;
            ServiceArea = null;
            LastLoginError = null;
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
