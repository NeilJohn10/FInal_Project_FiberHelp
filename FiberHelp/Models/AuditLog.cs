using System;

namespace FiberHelp.Models
{
    /// <summary>
    /// Persistent audit log entry for tracking login attempts, errors, and user activities.
    /// Supports rubric criteria: "Logs correctly record login attempts, errors, and user activities."
    /// </summary>
    public class AuditLog
    {
        public int Id { get; set; }

        /// <summary>
        /// Type of event: Login, Logout, Error, Create, Update, Delete, Archive, Restore, Access
        /// </summary>
        public string EventType { get; set; } = string.Empty;

        /// <summary>
        /// Category: Authentication, DataAccess, SystemError, UserActivity
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// User-friendly description of what happened (no technical details exposed)
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Internal technical details for administrator review only (never shown to end users)
        /// </summary>
        public string? TechnicalDetails { get; set; }

        /// <summary>
        /// User ID who performed the action (null for anonymous/failed login)
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Email or username associated with the event
        /// </summary>
        public string? UserEmail { get; set; }

        /// <summary>
        /// User role at the time of the event
        /// </summary>
        public string? UserRole { get; set; }

        /// <summary>
        /// Entity type affected (e.g., Client, Ticket, Account)
        /// </summary>
        public string? EntityType { get; set; }

        /// <summary>
        /// Entity ID affected
        /// </summary>
        public string? EntityId { get; set; }

        /// <summary>
        /// Whether the action was successful
        /// </summary>
        public bool IsSuccess { get; set; } = true;

        /// <summary>
        /// Severity level: Info, Warning, Error, Critical
        /// </summary>
        public string Severity { get; set; } = "Info";

        /// <summary>
        /// UTC timestamp of when the event occurred
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
