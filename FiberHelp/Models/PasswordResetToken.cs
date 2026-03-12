using System;
using System.ComponentModel.DataAnnotations;

namespace FiberHelp.Models
{
    public class PasswordResetToken
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string AccountType { get; set; } = string.Empty; // User, Agent, Technician

        [Required]
        [MaxLength(200)]
        public string CodeHash { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UsedAt { get; set; }
        public int AttemptCount { get; set; } = 0;
    }
}
