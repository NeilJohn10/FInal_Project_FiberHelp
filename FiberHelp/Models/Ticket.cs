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
 
 // Stores customer Id or name (maps to Customer column)
 public string Customer { get; set; } = string.Empty;
 
 // Convenience display name (maps to CustomerName column)
 public string CustomerName { get; set; } = string.Empty;
 
 // Priority column (nullable). If DB does not yet have column add via script.
 public string Priority { get; set; } = "Medium";
 
 public string Status { get; set; } = "Open";
 
 // Maps to SQL column CreatedAt
 public DateTime Created { get; set; } = DateTime.UtcNow;
 }
}
