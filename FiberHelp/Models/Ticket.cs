using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace FiberHelp.Models
{
 public class Ticket
 {
 // Maps to SQL column TicketId (int identity)
 public int Id { get; set; }
 
 // Optional account linkage (maps to AccountId if present)
 public int? AccountId { get; set; }
 
 // Maps to SQL column Subject
 public string Title { get; set; } = string.Empty;
 
 // Client relationship (nullable)
 public string? ClientId { get; set; }
 
 // Convenience display name (maps to CustomerName column)
 public string ClientName { get; set; } = string.Empty;
 
 // Priority column (nullable). If DB does not yet have column add via script.
 public string Priority { get; set; } = "Medium";
 
 public string Status { get; set; } = "Open";
 
 // Maps to SQL column CreatedAt
 public DateTime Created { get; set; } = DateTime.UtcNow;
 
 // Technician assignment fields
 public string? AssignedAgentId { get; set; } // Agent who owns/created the ticket
 public string? AssignedTechnicianId { get; set; } // Technician assigned to resolve
 public string? ResolvedByTechnicianId { get; set; } // Technician who resolved it
 public DateTime? AssignedAt { get; set; } // When ticket was assigned to technician
 public DateTime? ResolvedAt { get; set; } // When ticket was resolved
 public string? ResolutionNotes { get; set; } // Notes from technician about resolution
 
 // Archive fields - for preserving resolved/closed tickets
 public bool IsArchived { get; set; } = false; // Soft delete/archive flag
 public DateTime? ArchivedAt { get; set; } // When ticket was archived
 public string? ArchivedByUserId { get; set; } // Who archived the ticket
 public DateTime? ClosedAt { get; set; } // When ticket was officially closed (after resolution verified)
 }
}
