using System;

namespace FiberHelp.Models
{
 public class Invoice
 {
 // maps to DB column InvoiceId (int identity)
 public int Id { get; set; }

 // maps to AccountId in DB
 public int? AccountId { get; set; }

 // maps to AmountDue
 public decimal AmountDue { get; set; }

 // maps to Status (e.g., Pending, Paid)
 public string Status { get; set; } = "Pending";

 // maps to IssueDate
 public DateTime IssueDate { get; set; } = DateTime.UtcNow;
 }
}