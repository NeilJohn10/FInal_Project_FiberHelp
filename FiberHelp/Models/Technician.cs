using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FiberHelp.Models
{
    [Table("Technicians")]
    public class Technician
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Phone { get; set; }

        [MaxLength(500)]
        public string? ServiceArea { get; set; }

        [MaxLength(100)]
        public string Department { get; set; } = "Field Operations";

        [MaxLength(200)]
        public string? Specialization { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsLocked { get; set; } = false;

        public int FailedLoginCount { get; set; } = 0;

        public DateTime? LockedUntil { get; set; }

        public int LockoutCount { get; set; } = 0;

        public DateTime? LastLoginAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? PasswordChangedAt { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        // Archive fields
        public bool IsArchived { get; set; } = false;
        public DateTime? ArchivedAt { get; set; }
        
        [MaxLength(100)]
        public string? ArchivedBy { get; set; }
    }
}
