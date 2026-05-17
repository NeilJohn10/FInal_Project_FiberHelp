using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FiberHelp.Data.context;
using FiberHelp.Data;
using FiberHelp.Models;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace FiberHelp.Services
{
    // Custom DbContext for syncing that allows explicit ID inserts
    public class SyncDbContext : devLocalContext
    {
        public SyncDbContext(DbContextOptions<devLocalContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Disable Identity generation for all integer Primary Keys
            // This forces EF Core to include the explicit IDs in the INSERT statements
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                var pk = entity.FindPrimaryKey();
                if (pk != null && pk.Properties.Count == 1 && pk.Properties[0].ClrType == typeof(int))
                {
                    pk.Properties[0].ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
                }
            }
        }
    }

    // Simple one-way sync: Cloud SQL -> Local SQL Server cache
    public class DataSyncService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        public DataSyncService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task SyncCloudToLocalAsync()
        {
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sync_log.txt");
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var online = scope.ServiceProvider.GetRequiredService<onlineContext>();
                
                // Get the options configured in MauiProgram and instantiate our special Sync context
                var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<devLocalContext>>();
                using var local = new SyncDbContext(options);

                // Check connectivity
                try { 
                    if (!online.Database.CanConnect()) { 
                        File.WriteAllText(logPath, "Cannot connect to cloud database."); 
                        return; 
                    } 
                } catch (Exception conEx) { 
                    File.WriteAllText(logPath, "Connect check failed: " + conEx.Message); 
                    return; 
                }

                File.WriteAllText(logPath, $"Starting sync at {DateTime.Now}...\n");

                // Load all data from cloud (no tracking)
                var accounts = await online.Accounts.AsNoTracking().ToListAsync();
                var clients = await online.Clients.AsNoTracking().ToListAsync();
                var tickets = await online.Tickets.AsNoTracking().ToListAsync();
                var invoices = await online.Invoices.AsNoTracking().ToListAsync();
                var users = await online.Users.AsNoTracking().ToListAsync();
                var agents = await online.Agents.AsNoTracking().ToListAsync();
                
                List<ClientFeedback> feedbacks = new List<ClientFeedback>();
                try {
                    feedbacks = await online.Set<ClientFeedback>().AsNoTracking().ToListAsync();
                    File.AppendAllText(logPath, $"Found {feedbacks.Count} feedbacks in cloud.\n");
                } catch (Exception fEx) {
                    File.AppendAllText(logPath, "Error fetching feedbacks: " + fEx.Message + "\n");
                }
                
                List<Technician> technicians = new List<Technician>();
                try { technicians = await online.Technicians.AsNoTracking().ToListAsync(); } catch { }

                using var tx = await local.Database.BeginTransactionAsync();
                try 
                {
                    // Clear local tables
                    await local.Database.ExecuteSqlRawAsync("DELETE FROM Tickets");
                    await local.Database.ExecuteSqlRawAsync("DELETE FROM Invoices");
                    await local.Database.ExecuteSqlRawAsync("DELETE FROM Clients");
                    await local.Database.ExecuteSqlRawAsync("DELETE FROM Accounts");
                    await local.Database.ExecuteSqlRawAsync("DELETE FROM Agents");
                    await local.Database.ExecuteSqlRawAsync("DELETE FROM Users");
                    try { await local.Database.ExecuteSqlRawAsync("DELETE FROM ClientFeedbacks"); } catch { }
                    try { await local.Database.ExecuteSqlRawAsync("DELETE FROM Technicians"); } catch { }

                    // Sync Accounts
                    if (accounts.Count > 0)
                    {
                        local.Accounts.AddRange(accounts.Select(a => new Account { 
                            Id = a.Id, AccountNumber = a.AccountNumber, Name = a.Name, Type = a.Type,
                            ServicePlan = a.ServicePlan, ServiceStatus = a.ServiceStatus, IsActive = a.IsActive,
                            ContactName = a.ContactName, ContactEmail = a.ContactEmail, ContactPhone = a.ContactPhone,
                            BillingAddress = a.BillingAddress, CreatedAt = a.CreatedAt
                        }));
                        await local.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Accounts ON");
                        await local.SaveChangesAsync();
                        await local.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Accounts OFF");
                    }

                    // Sync Clients (string Id, no IDENTITY_INSERT needed)
                    if (clients.Count > 0)
                    {
                        local.Clients.AddRange(clients.Select(c => new Client { 
                            Id = c.Id, Name = c.Name, Email = c.Email, Plan = c.Plan,
                            Status = c.Status, JoinDate = c.JoinDate, AccountId = c.AccountId
                        }));
                        await local.SaveChangesAsync();
                    }

                    // Sync Users (string Id, no IDENTITY_INSERT needed)
                    if (users.Count > 0)
                    {
                        local.Users.AddRange(users.Select(u => new User { 
                            Id = u.Id, Email = u.Email, PasswordHash = u.PasswordHash, Role = u.Role, FullName = u.FullName 
                        }));
                        await local.SaveChangesAsync();
                    }

                    // Sync Agents (string Id, no IDENTITY_INSERT needed)
                    if (agents.Count > 0)
                    {
                        local.Agents.AddRange(agents.Select(g => new Agent { 
                            Id = g.Id, Email = g.Email, PasswordHash = g.PasswordHash, FullName = g.FullName, Role = g.Role,
                            Department = g.Department, Phone = g.Phone, ServiceArea = g.ServiceArea, IsActive = g.IsActive,
                            IsLocked = g.IsLocked, FailedLoginCount = g.FailedLoginCount, CreatedAt = g.CreatedAt,
                            UpdatedAt = g.UpdatedAt, LastLoginAt = g.LastLoginAt, PasswordChangedAt = g.PasswordChangedAt
                        }));
                        await local.SaveChangesAsync();
                    }

                    // Sync Technicians (string Id, no IDENTITY_INSERT needed)
                    if (technicians.Count > 0)
                    {
                        local.Technicians.AddRange(technicians.Select(t => new Technician { 
                            Id = t.Id, Email = t.Email, PasswordHash = t.PasswordHash, FullName = t.FullName, Phone = t.Phone, 
                            ServiceArea = t.ServiceArea, Department = t.Department, Specialization = t.Specialization, 
                            IsActive = t.IsActive, IsLocked = t.IsLocked, FailedLoginCount = t.FailedLoginCount, 
                            CreatedAt = t.CreatedAt, UpdatedAt = t.UpdatedAt, LastLoginAt = t.LastLoginAt, 
                            PasswordChangedAt = t.PasswordChangedAt, Notes = t.Notes
                        }));
                        await local.SaveChangesAsync();
                    }

                    // Sync Feedbacks
                    if (feedbacks.Count > 0)
                    {
                        var feedbackToSave = feedbacks.Select(f => {
                            if (string.IsNullOrEmpty(f.HandledById))
                            {
                                Ticket ticket = null;

                                // 1. Try to find by TicketId
                                if (f.TicketId.HasValue)
                                {
                                    ticket = tickets.FirstOrDefault(t => t.Id == f.TicketId.Value);
                                }
                                // 2. Smart Link: If no TicketId but has ClientId, find latest ticket for client
                                else if (!string.IsNullOrEmpty(f.ClientId))
                                {
                                    string searchId = f.ClientId.Trim();
                                    
                                    // Try direct ClientId match
                                    ticket = tickets
                                        .Where(t => t.ClientId != null && t.ClientId.Equals(searchId, StringComparison.OrdinalIgnoreCase))
                                        .Where(t => !string.IsNullOrEmpty(t.AssignedTechnicianId))
                                        .OrderByDescending(t => t.ResolvedAt ?? t.Created)
                                        .FirstOrDefault();
                                    
                                    // If not found, try matching by Account Number (many users use these interchangeably)
                                    if (ticket == null)
                                    {
                                        var account = accounts.FirstOrDefault(a => a.AccountNumber != null && a.AccountNumber.Equals(searchId, StringComparison.OrdinalIgnoreCase));
                                        if (account != null)
                                        {
                                            ticket = tickets
                                                .Where(t => t.AccountId == account.Id && !string.IsNullOrEmpty(t.AssignedTechnicianId))
                                                .OrderByDescending(t => t.ResolvedAt ?? t.Created)
                                                .FirstOrDefault();
                                        }
                                    }
                                    
                                    // Also auto-link the TicketId to the feedback to make it complete
                                    if (ticket != null)
                                    {
                                        f.TicketId = ticket.Id;
                                        File.AppendAllText(logPath, $"[LinkSuccess] Linked feedback {f.Id} to ticket {ticket.Id} via ID {searchId}\n");
                                    }
                                    else
                                    {
                                        File.AppendAllText(logPath, $"[LinkFail] Could not find ticket for ID {searchId}\n");
                                    }
                                }

                                if (ticket != null && !string.IsNullOrEmpty(ticket.AssignedTechnicianId))
                                {
                                    f.HandledById = ticket.AssignedTechnicianId;
                                    // Try to find the actual name from our technicians list
                                    var tech = technicians.FirstOrDefault(t => t.Id == ticket.AssignedTechnicianId);
                                    if (tech != null) f.HandledByName = tech.FullName;
                                }
                            }

                            return new ClientFeedback {
                                Id = f.Id, TransactionProofId = f.TransactionProofId, TicketId = f.TicketId,
                                InvoiceId = f.InvoiceId, ClientId = f.ClientId, ClientName = f.ClientName,
                                Rating = f.Rating, ServiceQualityRating = f.ServiceQualityRating, ResponseTimeRating = f.ResponseTimeRating,
                                ProfessionalismRating = f.ProfessionalismRating, CommunicationRating = f.CommunicationRating, WouldRecommend = f.WouldRecommend,
                                Comments = f.Comments, FeedbackCategory = f.FeedbackCategory, HandledById = f.HandledById,
                                HandledByName = f.HandledByName, SubmittedAt = f.SubmittedAt, IsReviewed = f.IsReviewed,
                                ReviewedAt = f.ReviewedAt, ReviewedById = f.ReviewedById, ReviewNotes = f.ReviewNotes,
                                Status = f.Status, RequiresFollowUp = f.RequiresFollowUp, FollowUpNotes = f.FollowUpNotes,
                                FollowUpCompletedAt = f.FollowUpCompletedAt
                            };
                        }).ToList();

                        local.Set<ClientFeedback>().AddRange(feedbackToSave);
                        await local.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT ClientFeedbacks ON");
                        await local.SaveChangesAsync();
                        await local.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT ClientFeedbacks OFF");
                        File.AppendAllText(logPath, "Feedbacks synced and auto-linked to technicians.\n");
                    }

                    // Sync Tickets
                    if (tickets.Count > 0)
                    {
                        local.Tickets.AddRange(tickets.Select(t => new Ticket {
                            Id = t.Id, AccountId = t.AccountId, Title = t.Title, ClientId = t.ClientId,
                            ClientName = t.ClientName, Priority = t.Priority, Status = t.Status,
                            Created = t.Created, AssignedAgentId = t.AssignedAgentId, AssignedTechnicianId = t.AssignedTechnicianId,
                            AssignedAt = t.AssignedAt, ResolvedByTechnicianId = t.ResolvedByTechnicianId, ResolvedAt = t.ResolvedAt,
                            ResolutionNotes = t.ResolutionNotes, IsArchived = t.IsArchived, ArchivedAt = t.ArchivedAt,
                            ArchivedByUserId = t.ArchivedByUserId, ClosedAt = t.ClosedAt
                        }));
                        await local.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Tickets ON");
                        await local.SaveChangesAsync();
                        await local.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Tickets OFF");
                    }

                    // Sync Invoices
                    if (invoices.Count > 0)
                    {
                        local.Invoices.AddRange(invoices.Select(i => new Invoice {
                            Id = i.Id, AccountId = i.AccountId, AccountName = i.AccountName,
                            AmountDue = i.AmountDue, IssueDate = i.IssueDate, DueDate = i.DueDate,
                            Status = i.Status, PaymentRef = i.PaymentRef, PaidDate = i.PaidDate, Notes = i.Notes
                        }));
                        await local.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Invoices ON");
                        await local.SaveChangesAsync();
                        await local.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Invoices OFF");
                    }

                    await tx.CommitAsync();
                    File.AppendAllText(logPath, "Sync completed successfully.\n");
                }
                catch (Exception txEx)
                {
                    await tx.RollbackAsync();
                    File.AppendAllText(logPath, "Transaction failed: " + txEx.Message + (txEx.InnerException != null ? " Inner: " + txEx.InnerException.Message : "") + "\n");
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(logPath, "Fatal sync error: " + ex.Message + "\n");
            }
        }
    }
}
