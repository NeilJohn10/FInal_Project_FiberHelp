using System;
using System.ComponentModel.DataAnnotations;

namespace FiberHelp.Models
{
    /// <summary>
    /// Represents proof of a completed transaction/service in the CRM system.
    /// Generated when a ticket is resolved or an invoice is paid.
    /// </summary>
    public class TransactionProof
    {
        public int Id { get; set; }

        /// <summary>
        /// Unique reference number for the transaction proof (e.g., TXN-2025-000001)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string ReferenceNumber { get; set; } = string.Empty;

        /// <summary>
        /// Type of transaction: TicketResolution, PaymentConfirmation, ServiceCompletion
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string TransactionType { get; set; } = string.Empty;

        /// <summary>
        /// Related ticket ID (if applicable)
        /// </summary>
        public int? TicketId { get; set; }

        /// <summary>
        /// Related invoice ID (if applicable)
        /// </summary>
        public int? InvoiceId { get; set; }

        /// <summary>
        /// Client ID who received the service
        /// </summary>
        [MaxLength(50)]
        public string? ClientId { get; set; }

        /// <summary>
        /// Client name for display purposes
        /// </summary>
        [MaxLength(200)]
        public string? ClientName { get; set; }

        /// <summary>
        /// Account ID (if applicable)
        /// </summary>
        public int? AccountId { get; set; }

        /// <summary>
        /// Description of what was completed
        /// </summary>
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Amount involved (for payment transactions)
        /// </summary>
        public decimal? Amount { get; set; }

        /// <summary>
        /// ID of the agent/technician who completed the transaction
        /// </summary>
        [MaxLength(50)]
        public string? CompletedById { get; set; }

        /// <summary>
        /// Name of the agent/technician who completed the transaction
        /// </summary>
        [MaxLength(200)]
        public string? CompletedByName { get; set; }

        /// <summary>
        /// When the transaction was completed
        /// </summary>
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the proof record was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Status of the proof: Generated, Sent, Acknowledged
        /// </summary>
        [MaxLength(50)]
        public string Status { get; set; } = "Generated";

        /// <summary>
        /// When the client acknowledged/viewed the proof
        /// </summary>
        public DateTime? AcknowledgedAt { get; set; }

        /// <summary>
        /// Resolution notes or additional details
        /// </summary>
        [MaxLength(2000)]
        public string? Notes { get; set; }

        /// <summary>
        /// Digital signature or verification hash
        /// </summary>
        [MaxLength(500)]
        public string? VerificationCode { get; set; }

        /// <summary>
        /// Generates a unique reference number for the transaction
        /// </summary>
        public static string GenerateReferenceNumber(int sequence)
        {
            return $"TXN-{DateTime.UtcNow:yyyy}-{sequence:D6}";
        }
    }
}
