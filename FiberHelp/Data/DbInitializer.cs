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

            // Read admin credentials from environment variables (secure coding practice)
            var adminEmail = Environment.GetEnvironmentVariable("FIBERHELP_ADMIN_EMAIL")
                ?? "admin@fiberhelp.com";
            var adminPassword = Environment.GetEnvironmentVariable("FIBERHELP_ADMIN_PASSWORD");

            if (string.IsNullOrWhiteSpace(adminPassword))
            {
                System.Diagnostics.Debug.WriteLine("DbInitializer: FIBERHELP_ADMIN_PASSWORD environment variable not set. Skipping admin seed.");
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
    }
}
