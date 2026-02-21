using System;
using System.ComponentModel.DataAnnotations;

namespace FiberHelp.Models
{
    /// <summary>
    /// Represents client feedback after a service/transaction is completed.
    /// Clients can rate their experience and provide comments.
    /// </summary>
    public class ClientFeedback
    {
        public int Id { get; set; }

        /// <summary>
        /// Related transaction proof ID
        /// </summary>
        public int? TransactionProofId { get; set; }

        /// <summary>
        /// Related ticket ID (if feedback is for a ticket resolution)
        /// </summary>
        public int? TicketId { get; set; }

        /// <summary>
        /// Related invoice ID (if feedback is for a payment)
        /// </summary>
        public int? InvoiceId { get; set; }

        /// <summary>
        /// Client who provided the feedback
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Client name for display
        /// </summary>
        [MaxLength(200)]
        public string? ClientName { get; set; }

        /// <summary>
        /// Overall rating (1-5 stars)
        /// </summary>
        [Range(1, 5)]
        public int Rating { get; set; }

        /// <summary>
        /// Service quality rating (1-5)
        /// </summary>
        [Range(1, 5)]
        public int? ServiceQualityRating { get; set; }

        /// <summary>
        /// Response time rating (1-5)
        /// </summary>
        [Range(1, 5)]
        public int? ResponseTimeRating { get; set; }

        /// <summary>
        /// Staff professionalism rating (1-5)
        /// </summary>
        [Range(1, 5)]
        public int? ProfessionalismRating { get; set; }

        /// <summary>
        /// Communication rating (1-5)
        /// </summary>
        [Range(1, 5)]
        public int? CommunicationRating { get; set; }

        /// <summary>
        /// Would the client recommend the service? (NPS question)
        /// </summary>
        public bool? WouldRecommend { get; set; }

        /// <summary>
        /// Client's comments/feedback text
        /// </summary>
        [MaxLength(2000)]
        public string? Comments { get; set; }

        /// <summary>
        /// Category of feedback: ServiceQuality, Billing, Technical, General
        /// </summary>
        [MaxLength(50)]
        public string FeedbackCategory { get; set; } = "General";

        /// <summary>
        /// ID of technician/agent who handled the service (for performance tracking)
        /// </summary>
        [MaxLength(50)]
        public string? HandledById { get; set; }

        /// <summary>
        /// Name of technician/agent
        /// </summary>
        [MaxLength(200)]
        public string? HandledByName { get; set; }

        /// <summary>
        /// When the feedback was submitted
        /// </summary>
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Whether the feedback has been reviewed by management
        /// </summary>
        public bool IsReviewed { get; set; } = false;

        /// <summary>
        /// When the feedback was reviewed
        /// </summary>
        public DateTime? ReviewedAt { get; set; }

        /// <summary>
        /// Who reviewed the feedback
        /// </summary>
        [MaxLength(50)]
        public string? ReviewedById { get; set; }

        /// <summary>
        /// Internal notes from the review
        /// </summary>
        [MaxLength(1000)]
        public string? ReviewNotes { get; set; }

        /// <summary>
        /// Status: Pending, Reviewed, ActionRequired, Resolved
        /// </summary>
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Whether follow-up is required
        /// </summary>
        public bool RequiresFollowUp { get; set; } = false;

        /// <summary>
        /// Follow-up notes if required
        /// </summary>
        [MaxLength(1000)]
        public string? FollowUpNotes { get; set; }

        /// <summary>
        /// When follow-up was completed
        /// </summary>
        public DateTime? FollowUpCompletedAt { get; set; }

        /// <summary>
        /// Calculate average rating across all rating categories
        /// </summary>
        public double GetAverageRating()
        {
            var ratings = new List<int> { Rating };
            if (ServiceQualityRating.HasValue) ratings.Add(ServiceQualityRating.Value);
            if (ResponseTimeRating.HasValue) ratings.Add(ResponseTimeRating.Value);
            if (ProfessionalismRating.HasValue) ratings.Add(ProfessionalismRating.Value);
            if (CommunicationRating.HasValue) ratings.Add(CommunicationRating.Value);
            return ratings.Average();
        }

        /// <summary>
        /// Get a human-readable rating label
        /// </summary>
        public string GetRatingLabel()
        {
            return Rating switch
            {
                5 => "Excellent",
                4 => "Good",
                3 => "Average",
                2 => "Poor",
                1 => "Very Poor",
                _ => "Not Rated"
            };
        }
    }
}
