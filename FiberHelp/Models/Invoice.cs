using System;

namespace FiberHelp.Models
{
 public class Invoice
 {
 // maps to DB column InvoiceId (int identity)
 public int Id { get; set; }

 // maps to AccountId in DB
 public int? AccountId { get; set; } // nullable to allow seed/demo rows without account

 // maps to AccountName in DB
 public string? AccountName { get; set; }

 // NEW: Direct client link for service invoices
 public string? ClientId { get; set; }
 public string? ClientName { get; set; }

 // maps to Amount
 public decimal AmountDue { get; set; } // Align with existing usage and EF mapping

 // maps to IssueDate
 public DateTime IssueDate { get; set; }

 // maps to DueDate
 public DateTime? DueDate { get; set; }

 // maps to Status (e.g., Pending, Paid)
 public string Status { get; set; } = "Unpaid"; // Pending/Paid/Unpaid

 // maps to PaymentRef
 public string? PaymentRef { get; set; }

 // maps to PaidDate
 public DateTime? PaidDate { get; set; }

 // maps to Notes
 public string? Notes { get; set; }

 // NEW: Link to service ticket (for service-related invoices)
 public int? RelatedTicketId { get; set; }

 // Invoice type: Subscription, Service, OneTime
 public string InvoiceType { get; set; } = "Subscription";
 }
}