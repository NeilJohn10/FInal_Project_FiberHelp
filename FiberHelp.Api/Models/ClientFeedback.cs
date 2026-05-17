using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;

namespace FiberHelp.Api.Models
{
    public class ClientFeedback
    {
        public int Id { get; set; }
        public int? TransactionProofId { get; set; }
        public int? TicketId { get; set; }
        public int? InvoiceId { get; set; }
        [Required]
        [MaxLength(50)]
        public string ClientId { get; set; } = string.Empty;
        [MaxLength(200)]
        public string? ClientName { get; set; }
        [Range(1, 5)]
        public int Rating { get; set; }
        [Range(1, 5)]
        public int? ServiceQualityRating { get; set; }
        [Range(1, 5)]
        public int? ResponseTimeRating { get; set; }
        [Range(1, 5)]
        public int? ProfessionalismRating { get; set; }
        [Range(1, 5)]
        public int? CommunicationRating { get; set; }
        public bool? WouldRecommend { get; set; }
        [MaxLength(2000)]
        public string? Comments { get; set; }
        [MaxLength(50)]
        public string FeedbackCategory { get; set; } = "General";
        [MaxLength(50)]
        public string? HandledById { get; set; }
        [MaxLength(200)]
        public string? HandledByName { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public bool IsReviewed { get; set; } = false;
        public DateTime? ReviewedAt { get; set; }
        [MaxLength(50)]
        public string? ReviewedById { get; set; }
        [MaxLength(1000)]
        public string? ReviewNotes { get; set; }
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";
        public bool RequiresFollowUp { get; set; } = false;
        [MaxLength(1000)]
        public string? FollowUpNotes { get; set; }
        public DateTime? FollowUpCompletedAt { get; set; }
    }
}
