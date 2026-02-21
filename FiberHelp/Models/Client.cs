using System;
using System.Text.Json.Serialization;

namespace FiberHelp.Models
{
    public class Client
    {
        public string Id { get; set; } = string.Empty; // Will be set by CreateCustomerAsync
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Plan { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public DateTime JoinDate { get; set; } = DateTime.UtcNow;
        public int? AccountId { get; set; }
        
        [JsonIgnore]
        public Account? Account { get; set; }

        // Archive fields (similar to Ticket archive support)
        public bool IsArchived { get; set; } = false;
        public DateTime? ArchivedAt { get; set; }
        public string? ArchivedByUserId { get; set; }

        /// <summary>
        /// Generates a user-friendly Client ID like CLT-0001
        /// </summary>
        public static string GenerateClientId(int sequence)
        {
            return $"CLT-{sequence:D4}";
        }
    }
}
