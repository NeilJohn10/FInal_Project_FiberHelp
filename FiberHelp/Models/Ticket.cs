using System;

namespace FiberHelp.Models
{
 public class Ticket
 {
 public string Id { get; set; } = Guid.NewGuid().ToString();
 public string Title { get; set; } = string.Empty;
 public string Customer { get; set; } = string.Empty;
 public string Priority { get; set; } = string.Empty;
 public string Status { get; set; } = string.Empty;
 public DateTime Created { get; set; } = DateTime.UtcNow;
 }
}
