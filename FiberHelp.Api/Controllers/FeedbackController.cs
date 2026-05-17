using Microsoft.AspNetCore.Mvc;
using FiberHelp.Api.Models;
using FiberHelp.Api.Data;
using System.Threading.Tasks;

namespace FiberHelp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeedbackController : ControllerBase
    {
        private readonly FeedbackDbContext _db;

        public FeedbackController(FeedbackDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// DTO mapping for external systems submitting feedback
        /// </summary>
        public class FeedbackRequest
        {
            public int? TicketId { get; set; }
            public string ClientId { get; set; } = string.Empty;
            public string? ClientName { get; set; }
            public int Rating { get; set; }
            public int? ServiceQualityRating { get; set; }
            public int? ResponseTimeRating { get; set; }
            public int? ProfessionalismRating { get; set; }
            public int? CommunicationRating { get; set; }
            public bool? WouldRecommend { get; set; }
            public string? Comments { get; set; }
            public string FeedbackCategory { get; set; } = "General";
        }

        /// <summary>
        /// POST: api/feedback
        /// Receives feedback from the Client Self-Service platform.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SubmitFeedback([FromBody] FeedbackRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (request.Rating < 1 || request.Rating > 5)
            {
                return BadRequest(new { error = "Rating must be between 1 and 5 stars." });
            }

            if (string.IsNullOrWhiteSpace(request.ClientId))
            {
                return BadRequest(new { error = "ClientId is required." });
            }

            var feedback = new ClientFeedback
            {
                TicketId = request.TicketId,
                ClientId = request.ClientId,
                ClientName = request.ClientName ?? request.ClientId,
                Rating = request.Rating,
                ServiceQualityRating = request.ServiceQualityRating,
                ResponseTimeRating = request.ResponseTimeRating,
                ProfessionalismRating = request.ProfessionalismRating,
                CommunicationRating = request.CommunicationRating,
                WouldRecommend = request.WouldRecommend,
                Comments = request.Comments,
                FeedbackCategory = request.FeedbackCategory ?? "General"
            };

            try 
            {
                _db.ClientFeedbacks.Add(feedback);
                await _db.SaveChangesAsync();

                return Ok(new { 
                    success = true, 
                    message = "Feedback submitted successfully.", 
                    feedbackId = feedback.Id 
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { error = "Failed to process feedback submission.", details = ex.Message });
            }
        }
    }
}
