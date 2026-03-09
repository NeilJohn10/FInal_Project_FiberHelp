using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FiberHelp.Data;
using FiberHelp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FiberHelp.Services
{
    /// <summary>
    /// Service for handling transaction validation, proof generation, and client feedback.
    /// </summary>
    public class TransactionService
    {
        private readonly AppDbContext _db;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ErrorHandlingService _errorService;
        public string? LastError { get; private set; }

        public TransactionService(AppDbContext db, IServiceScopeFactory scopeFactory, ErrorHandlingService errorService)
        {
            _db = db;
            _scopeFactory = scopeFactory;
            _errorService = errorService;
        }

        /// <summary>
        /// Returns a user-friendly error message without leaking internal details.
        /// </summary>
        private string SanitizeError(Exception ex)
        {
            return _errorService.GetUserFriendlyMessage(ex);
        }

        #region Transaction Proof Methods

        /// <summary>
        /// Generate proof of transaction when a ticket is resolved
        /// </summary>
        public async Task<TransactionProof?> GenerateTicketResolutionProofAsync(
            int ticketId,
            string? technicianId = null,
            string? technicianName = null,
            string? resolutionNotes = null)
        {
            try
            {
                LastError = null;

                var ticket = await _db.Tickets.FindAsync(ticketId);
                if (ticket == null)
                {
                    LastError = "Ticket not found";
                    return null;
                }

                // Check if proof already exists for this ticket
                var existingProof = await _db.Set<TransactionProof>()
                    .FirstOrDefaultAsync(p => p.TicketId == ticketId && p.TransactionType == "TicketResolution");

                if (existingProof != null)
                {
                    LastError = "Proof already generated for this ticket";
                    return existingProof;
                }

                // Generate sequence number
                var maxId = await _db.Set<TransactionProof>().MaxAsync(p => (int?)p.Id) ?? 0;
                var referenceNumber = TransactionProof.GenerateReferenceNumber(maxId + 1);

                var proof = new TransactionProof
                {
                    ReferenceNumber = referenceNumber,
                    TransactionType = "TicketResolution",
                    TicketId = ticketId,
                    ClientId = ticket.ClientId,
                    ClientName = ticket.ClientName,
                    AccountId = ticket.AccountId,
                    Description = $"Service ticket '{ticket.Title}' has been resolved.",
                    CompletedById = technicianId ?? ticket.ResolvedByTechnicianId,
                    CompletedByName = technicianName,
                    CompletedAt = ticket.ResolvedAt ?? DateTime.UtcNow,
                    Notes = resolutionNotes ?? ticket.ResolutionNotes,
                    Status = "Generated",
                    VerificationCode = GenerateVerificationCode(referenceNumber, ticketId.ToString())
                };

                _db.Set<TransactionProof>().Add(proof);
                await _db.SaveChangesAsync();

                // Sync to cloud
                await EnqueueSyncAsync("Create", proof);

                System.Diagnostics.Debug.WriteLine($"[TransactionService] Generated proof {referenceNumber} for ticket {ticketId}");
                return proof;
            }
            catch (Exception ex)
            {
                LastError = SanitizeError(ex);
                System.Diagnostics.Debug.WriteLine($"[TransactionService] GenerateTicketResolutionProofAsync error: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Generate proof of payment when an invoice is paid
        /// </summary>
        public async Task<TransactionProof?> GeneratePaymentProofAsync(
            int invoiceId,
            string? processedById = null,
            string? processedByName = null)
        {
            try
            {
                LastError = null;

                var invoice = await _db.Invoices.FindAsync(invoiceId);
                if (invoice == null)
                {
                    LastError = "Invoice not found";
                    return null;
                }

                if (invoice.Status != "Paid")
                {
                    LastError = "Invoice is not marked as paid";
                    return null;
                }

                // Check if proof already exists
                var existingProof = await _db.Set<TransactionProof>()
                    .FirstOrDefaultAsync(p => p.InvoiceId == invoiceId && p.TransactionType == "PaymentConfirmation");

                if (existingProof != null)
                {
                    return existingProof;
                }

                var maxId = await _db.Set<TransactionProof>().MaxAsync(p => (int?)p.Id) ?? 0;
                var referenceNumber = TransactionProof.GenerateReferenceNumber(maxId + 1);

                var proof = new TransactionProof
                {
                    ReferenceNumber = referenceNumber,
                    TransactionType = "PaymentConfirmation",
                    InvoiceId = invoiceId,
                    TicketId = invoice.RelatedTicketId,
                    ClientId = invoice.ClientId,
                    ClientName = invoice.ClientName,
                    AccountId = invoice.AccountId,
                    Description = $"Payment received for invoice. Amount: ?{invoice.AmountDue:N2}",
                    Amount = invoice.AmountDue,
                    CompletedById = processedById,
                    CompletedByName = processedByName,
                    CompletedAt = invoice.PaidDate ?? DateTime.UtcNow,
                    Notes = $"Payment Reference: {invoice.PaymentRef}",
                    Status = "Generated",
                    VerificationCode = GenerateVerificationCode(referenceNumber, invoiceId.ToString())
                };

                _db.Set<TransactionProof>().Add(proof);
                await _db.SaveChangesAsync();

                await EnqueueSyncAsync("Create", proof);

                System.Diagnostics.Debug.WriteLine($"[TransactionService] Generated payment proof {referenceNumber} for invoice {invoiceId}");
                return proof;
            }
            catch (Exception ex)
            {
                LastError = SanitizeError(ex);
                System.Diagnostics.Debug.WriteLine($"[TransactionService] GeneratePaymentProofAsync error: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Get all transaction proofs for a client
        /// </summary>
        public async Task<List<TransactionProof>> GetClientTransactionProofsAsync(string clientId)
        {
            try
            {
                LastError = null;
                return await _db.Set<TransactionProof>()
                    .Where(p => p.ClientId == clientId)
                    .OrderByDescending(p => p.CompletedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                LastError = SanitizeError(ex);
                return new List<TransactionProof>();
            }
        }

        /// <summary>
        /// Get transaction proof by reference number
        /// </summary>
        public async Task<TransactionProof?> GetProofByReferenceAsync(string referenceNumber)
        {
            try
            {
                LastError = null;
                return await _db.Set<TransactionProof>()
                    .FirstOrDefaultAsync(p => p.ReferenceNumber == referenceNumber);
            }
            catch (Exception ex)
            {
                LastError = SanitizeError(ex);
                return null;
            }
        }

        /// <summary>
        /// Validate a transaction proof using verification code
        /// </summary>
        public async Task<bool> ValidateProofAsync(string referenceNumber, string verificationCode)
        {
            try
            {
                var proof = await GetProofByReferenceAsync(referenceNumber);
                if (proof == null) return false;

                return proof.VerificationCode == verificationCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Mark proof as acknowledged by client
        /// </summary>
        public async Task<bool> AcknowledgeProofAsync(int proofId)
        {
            try
            {
                LastError = null;
                var proof = await _db.Set<TransactionProof>().FindAsync(proofId);
                if (proof == null)
                {
                    LastError = "Proof not found";
                    return false;
                }

                proof.Status = "Acknowledged";
                proof.AcknowledgedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                await EnqueueSyncAsync("Update", proof);
                return true;
            }
            catch (Exception ex)
            {
                LastError = SanitizeError(ex);
                return false;
            }
        }

        /// <summary>
        /// Get all transaction proofs with optional filtering
        /// </summary>
        public async Task<List<TransactionProof>> GetAllProofsAsync(
            string? transactionType = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            try
            {
                LastError = null;
                var query = _db.Set<TransactionProof>().AsQueryable();

                if (!string.IsNullOrWhiteSpace(transactionType))
                    query = query.Where(p => p.TransactionType == transactionType);

                if (fromDate.HasValue)
                    query = query.Where(p => p.CompletedAt >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(p => p.CompletedAt <= toDate.Value);

                return await query.OrderByDescending(p => p.CompletedAt).ToListAsync();
            }
            catch (Exception ex)
            {
                LastError = SanitizeError(ex);
                return new List<TransactionProof>();
            }
        }

        #endregion

        #region Client Feedback Methods

        /// <summary>
        /// Submit client feedback for a transaction
        /// </summary>
        public async Task<ClientFeedback?> SubmitFeedbackAsync(ClientFeedback feedback)
        {
            try
            {
                LastError = null;

                // Validate required fields
                if (string.IsNullOrWhiteSpace(feedback.ClientId))
                {
                    LastError = "Client ID is required";
                    return null;
                }

                if (feedback.Rating < 1 || feedback.Rating > 5)
                {
                    LastError = "Rating must be between 1 and 5";
                    return null;
                }

                // Check for duplicate feedback
                if (feedback.TicketId.HasValue)
                {
                    var existingFeedback = await _db.Set<ClientFeedback>()
                        .FirstOrDefaultAsync(f => f.TicketId == feedback.TicketId && f.ClientId == feedback.ClientId);

                    if (existingFeedback != null)
                    {
                        LastError = "Feedback already submitted for this ticket";
                        return existingFeedback;
                    }
                }

                feedback.SubmittedAt = DateTime.UtcNow;
                feedback.Status = "Pending";

                _db.Set<ClientFeedback>().Add(feedback);
                await _db.SaveChangesAsync();

                await EnqueueSyncAsync("Create", feedback);

                System.Diagnostics.Debug.WriteLine($"[TransactionService] Feedback submitted by client {feedback.ClientId}");
                return feedback;
            }
            catch (Exception ex)
            {
                LastError = SanitizeError(ex);
                System.Diagnostics.Debug.WriteLine($"[TransactionService] SubmitFeedbackAsync error: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Get all feedback for a specific ticket
        /// </summary>
        public async Task<ClientFeedback?> GetTicketFeedbackAsync(int ticketId)
        {
            try
            {
                LastError = null;
                return await _db.Set<ClientFeedback>()
                    .FirstOrDefaultAsync(f => f.TicketId == ticketId);
            }
            catch (Exception ex)
            {
                LastError = SanitizeError(ex);
                return null;
            }
        }

        /// <summary>
        /// Get all feedback from a client
        /// </summary>
        public async Task<List<ClientFeedback>> GetClientFeedbackHistoryAsync(string clientId)
        {
            try
            {
                LastError = null;
                return await _db.Set<ClientFeedback>()
                    .Where(f => f.ClientId == clientId)
                    .OrderByDescending(f => f.SubmittedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                LastError = SanitizeError(ex);
                return new List<ClientFeedback>();
            }
        }

        /// <summary>
        /// Get all feedback (for admin review)
        /// </summary>
        public async Task<List<ClientFeedback>> GetAllFeedbackAsync(
            bool? onlyPending = null,
            bool? requiresFollowUp = null)
        {
            try
            {
                LastError = null;
                var query = _db.Set<ClientFeedback>().AsQueryable();

                if (onlyPending == true)
                    query = query.Where(f => f.Status == "Pending");

                if (requiresFollowUp == true)
                    query = query.Where(f => f.RequiresFollowUp && !f.FollowUpCompletedAt.HasValue);

                return await query.OrderByDescending(f => f.SubmittedAt).ToListAsync();
            }
            catch (Exception ex)
            {
                LastError = SanitizeError(ex);
                return new List<ClientFeedback>();
            }
        }

        /// <summary>
        /// Mark feedback as reviewed
        /// </summary>
        public async Task<bool> ReviewFeedbackAsync(
            int feedbackId,
            string reviewedById,
            string? reviewNotes = null,
            bool requiresFollowUp = false)
        {
            try
            {
                LastError = null;
                var feedback = await _db.Set<ClientFeedback>().FindAsync(feedbackId);
                if (feedback == null)
                {
                    LastError = "Feedback not found";
                    return false;
                }

                feedback.IsReviewed = true;
                feedback.ReviewedAt = DateTime.UtcNow;
                feedback.ReviewedById = reviewedById;
                feedback.ReviewNotes = reviewNotes;
                feedback.RequiresFollowUp = requiresFollowUp;
                feedback.Status = requiresFollowUp ? "ActionRequired" : "Reviewed";

                await _db.SaveChangesAsync();
                await EnqueueSyncAsync("Update", feedback);

                return true;
            }
            catch (Exception ex)
            {
                LastError = SanitizeError(ex);
                return false;
            }
        }

        /// <summary>
        /// Complete follow-up for feedback
        /// </summary>
        public async Task<bool> CompleteFollowUpAsync(int feedbackId, string followUpNotes)
        {
            try
            {
                LastError = null;
                var feedback = await _db.Set<ClientFeedback>().FindAsync(feedbackId);
                if (feedback == null)
                {
                    LastError = "Feedback not found";
                    return false;
                }

                feedback.FollowUpNotes = followUpNotes;
                feedback.FollowUpCompletedAt = DateTime.UtcNow;
                feedback.Status = "Resolved";

                await _db.SaveChangesAsync();
                await EnqueueSyncAsync("Update", feedback);

                return true;
            }
            catch (Exception ex)
            {
                LastError = SanitizeError(ex);
                return false;
            }
        }

        /// <summary>
        /// Get feedback statistics for dashboard
        /// </summary>
        public async Task<FeedbackStatistics> GetFeedbackStatisticsAsync(DateTime? fromDate = null)
        {
            try
            {
                LastError = null;
                var query = _db.Set<ClientFeedback>().AsQueryable();

                if (fromDate.HasValue)
                    query = query.Where(f => f.SubmittedAt >= fromDate.Value);

                var feedbackList = await query.ToListAsync();

                if (!feedbackList.Any())
                {
                    return new FeedbackStatistics(0, 0, 0, 0, 0, 0, 0, new Dictionary<int, int>());
                }

                var totalCount = feedbackList.Count;
                var averageRating = feedbackList.Average(f => f.Rating);
                var pendingCount = feedbackList.Count(f => f.Status == "Pending");
                var reviewedCount = feedbackList.Count(f => f.IsReviewed);
                var requiresFollowUpCount = feedbackList.Count(f => f.RequiresFollowUp && !f.FollowUpCompletedAt.HasValue);
                var wouldRecommendCount = feedbackList.Count(f => f.WouldRecommend == true);
                var wouldRecommendPercentage = totalCount > 0 
                    ? (double)wouldRecommendCount / feedbackList.Count(f => f.WouldRecommend.HasValue) * 100 
                    : 0;

                var ratingDistribution = feedbackList
                    .GroupBy(f => f.Rating)
                    .ToDictionary(g => g.Key, g => g.Count());

                return new FeedbackStatistics(
                    totalCount,
                    Math.Round(averageRating, 2),
                    pendingCount,
                    reviewedCount,
                    requiresFollowUpCount,
                    wouldRecommendCount,
                    Math.Round(wouldRecommendPercentage, 1),
                    ratingDistribution
                );
            }
            catch (Exception ex)
            {
                LastError = SanitizeError(ex);
                return new FeedbackStatistics(0, 0, 0, 0, 0, 0, 0, new Dictionary<int, int>());
            }
        }

        /// <summary>
        /// Get technician/agent performance based on feedback
        /// </summary>
        public async Task<List<StaffPerformance>> GetStaffPerformanceAsync()
        {
            try
            {
                LastError = null;
                var feedbackList = await _db.Set<ClientFeedback>()
                    .Where(f => !string.IsNullOrEmpty(f.HandledById))
                    .ToListAsync();

                var performance = feedbackList
                    .GroupBy(f => new { f.HandledById, f.HandledByName })
                    .Select(g => new StaffPerformance(
                        g.Key.HandledById ?? "",
                        g.Key.HandledByName ?? "Unknown",
                        g.Count(),
                        Math.Round(g.Average(f => f.Rating), 2),
                        g.Count(f => f.Rating >= 4),
                        g.Count(f => f.Rating <= 2)
                    ))
                    .OrderByDescending(p => p.AverageRating)
                    .ToList();

                return performance;
            }
            catch (Exception ex)
            {
                LastError = SanitizeError(ex);
                return new List<StaffPerformance>();
            }
        }

        #endregion

        #region Helper Methods

        private string GenerateVerificationCode(string referenceNumber, string entityId)
        {
            var input = $"{referenceNumber}-{entityId}-{DateTime.UtcNow:yyyyMMdd}";
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes)[..16].ToUpperInvariant();
        }

        private async Task EnqueueSyncAsync<T>(string operation, T entity) where T : class
        {
            try
            {
                var dualWrite = new DualWriteService(_scopeFactory);
                await dualWrite.EnqueueOnlyAsync(operation, entity);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TransactionService] Failed to enqueue sync: {ex.Message}");
            }
        }

        #endregion

        #region Record Types

        public sealed record FeedbackStatistics(
            int TotalCount,
            double AverageRating,
            int PendingCount,
            int ReviewedCount,
            int RequiresFollowUpCount,
            int WouldRecommendCount,
            double WouldRecommendPercentage,
            IReadOnlyDictionary<int, int> RatingDistribution
        );

        public sealed record StaffPerformance(
            string StaffId,
            string StaffName,
            int FeedbackCount,
            double AverageRating,
            int PositiveFeedbackCount,
            int NegativeFeedbackCount
        );

        #endregion
    }
}
