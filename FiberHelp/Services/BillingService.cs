using System.Collections.Generic;
using System.Threading.Tasks;
using FiberHelp.Data;
using FiberHelp.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace FiberHelp.Services
{
 public class BillingService
 {
 private readonly AppDbContext _db;
 private readonly IServiceScopeFactory _scopeFactory;
 private readonly ErrorHandlingService _errorService;
 public string? LastError { get; private set; }

 public BillingService(AppDbContext db, IServiceScopeFactory scopeFactory, ErrorHandlingService errorService) 
 { 
     _db = db; 
     _scopeFactory = scopeFactory;
     _errorService = errorService;
 }

 /// <summary>
 /// Returns a user-friendly error message without leaking internal details.
 /// </summary>
 private string SanitizeError(Exception ex)
 {
     return _errorService.GetUserFriendlyMessage(ex);
 }

 public async Task<List<Invoice>> GetInvoicesAsync()
 {
 try
 {
 LastError = null;
 var list = await _db.Invoices.OrderByDescending(i => i.IssueDate).ToListAsync();
 await NormalizeMissingFieldsAsync(list);
 await UpdateOverdueStatusAsync(list); // Auto-update overdue invoices
 return list.OrderByDescending(i => i.IssueDate).ToList();
 }
 catch (Exception ex)
 {
 LastError = SanitizeError(ex);
 System.Diagnostics.Debug.WriteLine($"BillingService.GetInvoicesAsync error: {ex}");
 return new List<Invoice>();
 }
 }

 /// <summary>
 /// Automatically marks pending invoices as Overdue if past due date
 /// </summary>
 private async Task UpdateOverdueStatusAsync(List<Invoice> list)
 {
     if (list.Count == 0) return;
     var now = DateTime.UtcNow;
     bool changed = false;

     foreach (var inv in list)
     {
         // Skip paid invoices
         if (string.Equals(inv.Status, "Paid", StringComparison.OrdinalIgnoreCase))
             continue;

         var dueDate = inv.DueDate ?? inv.IssueDate.AddDays(30);
         
         // If past due and not already marked overdue
         if (dueDate < now && !string.Equals(inv.Status, "Overdue", StringComparison.OrdinalIgnoreCase))
         {
             inv.Status = "Overdue";
             changed = true;
         }
         // If not yet due but marked overdue (edge case fix), revert to pending
         else if (dueDate >= now && string.Equals(inv.Status, "Overdue", StringComparison.OrdinalIgnoreCase))
         {
             inv.Status = "Pending";
             changed = true;
         }
     }

     if (changed)
     {
         try { await _db.SaveChangesAsync(); }
         catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"UpdateOverdueStatusAsync save error: {ex}"); }
     }
 }

 private async Task NormalizeMissingFieldsAsync(List<Invoice> list)
 {
 if (list.Count == 0) return;
 bool changed = false;
 foreach (var inv in list)
 {
 // Fill AccountName if missing and AccountId present
 if (inv.AccountName == null && inv.AccountId.HasValue)
 {
 var acct = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == inv.AccountId.Value);
 if (acct != null)
 {
 inv.AccountName = acct.Name;
 changed = true;
 }
 }
 // Add DueDate default (30 days after issue) if null
 if (!inv.DueDate.HasValue)
 {
 inv.DueDate = inv.IssueDate.AddDays(30);
 changed = true;
 }
 // If Paid but PaidDate missing -> use IssueDate or now
 if (string.Equals(inv.Status, "Paid", StringComparison.OrdinalIgnoreCase) && !inv.PaidDate.HasValue)
 {
 inv.PaidDate = DateTime.UtcNow < inv.IssueDate ? inv.IssueDate : DateTime.UtcNow;
 changed = true;
 }
 // Generate PaymentRef if paid and missing
 if (string.Equals(inv.Status, "Paid", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(inv.PaymentRef))
 {
 inv.PaymentRef = $"PAY-{inv.Id:000000}";
 changed = true;
 }
 // Add default notes if empty
 if (string.IsNullOrWhiteSpace(inv.Notes))
 {
 inv.Notes = "System auto-filled missing data";
 changed = true;
 }
 }
 if (changed)
 {
 try { await _db.SaveChangesAsync(); }
 catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"NormalizeMissingFieldsAsync save error: {ex}"); }
 }
 }

 public async Task<Invoice?> GetInvoiceAsync(int id)
 {
 try { LastError = null; return await _db.Invoices.FindAsync(id); }
 catch (Exception ex) { LastError = SanitizeError(ex); System.Diagnostics.Debug.WriteLine($"BillingService.GetInvoiceAsync error: {ex}"); return null; }
 }

 public async Task<bool> CreateAsync(Invoice inv)
 {
 try
 {
 LastError = null;
 _db.Invoices.Add(inv);
 var ok = await _db.SaveChangesAsync() > 0;
 
 // Sync to cloud via outbox
 if (ok)
 {
     try
     {
         var dualWrite = new DualWriteService(_scopeFactory);
         await dualWrite.EnqueueOnlyAsync("Create", inv, inv.Id.ToString());
     }
     catch (Exception ex)
     {
         System.Diagnostics.Debug.WriteLine($"Failed to enqueue invoice sync: {ex.Message}");
         // Don't fail the create, sync will retry
     }
 }
 
 return ok;
 }
 catch (Exception ex)
 {
 LastError = SanitizeError(ex);
 System.Diagnostics.Debug.WriteLine($"BillingService.CreateAsync error: {ex}");
 return false;
 }
 }

 public async Task<bool> MarkPaidAsync(int id)
 {
 try
 {
 LastError = null;
 var inv = await _db.Invoices.FindAsync(id);
 if (inv == null) return false;
 inv.Status = "Paid";
 inv.PaidDate = DateTime.UtcNow;
 if (string.IsNullOrWhiteSpace(inv.PaymentRef)) inv.PaymentRef = $"PAY-{inv.Id:000000}";
 _db.Invoices.Update(inv);
 await _db.SaveChangesAsync();
 
 // Sync to cloud via outbox
 try
 {
     var dualWrite = new DualWriteService(_scopeFactory);
     await dualWrite.EnqueueOnlyAsync("Update", inv, inv.Id.ToString());
 }
 catch (Exception ex)
 {
     System.Diagnostics.Debug.WriteLine($"Failed to enqueue invoice sync: {ex.Message}");
 }
 
 return true;
 }
 catch (Exception ex)
 {
 LastError = SanitizeError(ex);
 System.Diagnostics.Debug.WriteLine($"BillingService.MarkPaidAsync error: {ex}");
 return false;
 }
 }

 // Accounting rubric #3 additions -----------------------------
 public sealed record AccountingSummary(
 int TotalInvoices,
 int PaidCount,
 int PendingCount,
 decimal TotalAmount,
 decimal PaidAmount,
 decimal PendingAmount,
 int OverdueCount,
 decimal OverdueAmount,
 IReadOnlyDictionary<string,int> AgingBuckets);

 public async Task<AccountingSummary> GetSummaryAsync(DateTime? asOf = null)
 {
 var now = asOf ?? DateTime.UtcNow;
 var all = await _db.Invoices.AsNoTracking().ToListAsync();
 var totalAmount = all.Sum(i => i.AmountDue);
 var paid = all.Where(i => string.Equals(i.Status, "Paid", StringComparison.OrdinalIgnoreCase)).ToList();
 var pending = all.Where(i => !string.Equals(i.Status, "Paid", StringComparison.OrdinalIgnoreCase)).ToList();
 var overdue = pending.Where(i => (i.DueDate ?? i.IssueDate.AddDays(30)) < now).ToList();

 // Simple aging buckets based on days past due
 var buckets = new Dictionary<string,int>
 {
 { "Current", pending.Count(i => (i.DueDate ?? i.IssueDate.AddDays(30)) >= now) },
 { "1-30", overdue.Count(i => DaysPastDue(i, now) is >=1 and <=30) },
 { "31-60", overdue.Count(i => DaysPastDue(i, now) is >=31 and <=60) },
 { "61-90", overdue.Count(i => DaysPastDue(i, now) is >=61 and <=90) },
 { ">90", overdue.Count(i => DaysPastDue(i, now) > 90) }
 };

 return new AccountingSummary(
 TotalInvoices: all.Count,
 PaidCount: paid.Count,
 PendingCount: pending.Count,
 TotalAmount: totalAmount,
 PaidAmount: paid.Sum(i => i.AmountDue),
 PendingAmount: pending.Sum(i => i.AmountDue),
 OverdueCount: overdue.Count,
 OverdueAmount: overdue.Sum(i => i.AmountDue),
 AgingBuckets: buckets
 );
 }

 private static int DaysPastDue(Invoice i, DateTime now)
 {
 var due = i.DueDate ?? i.IssueDate.AddDays(30);
 return (int)Math.Max(0, (now - due).TotalDays);
 }

 // Apply a payment with basic automation (status/ref/date)
 public async Task<bool> ApplyPaymentAsync(int invoiceId, string paymentRef, DateTime? paidDate = null)
 {
 try
 {
 var inv = await _db.Invoices.FindAsync(invoiceId);
 if (inv == null) return false;
 inv.Status = "Paid";
 inv.PaymentRef = string.IsNullOrWhiteSpace(paymentRef) ? $"PAY-{invoiceId:000000}" : paymentRef.Trim();
 inv.PaidDate = paidDate ?? DateTime.UtcNow;
 _db.Invoices.Update(inv);
 await _db.SaveChangesAsync();
 return true;
 }
 catch (Exception ex)
 {
 LastError = SanitizeError(ex);
 System.Diagnostics.Debug.WriteLine($"BillingService.ApplyPaymentAsync error: {ex}");
 return false;
 }
 }

 // Expenses ---------------------------------------------------
 public async Task<List<Expense>> GetExpensesAsync()
 {
 try { return await _db.Expenses.OrderByDescending(e => e.Date).ToListAsync(); }
 catch (Exception ex) { LastError = SanitizeError(ex); return new List<Expense>(); }
 }

 public async Task<bool> AddExpenseAsync(Expense e)
 {
 try 
 { 
     LastError = null;
     _db.Expenses.Add(e); 
     var ok = await _db.SaveChangesAsync() > 0;
     
     // Sync to cloud via outbox
     if (ok)
     {
         try
         {
             var dualWrite = new DualWriteService(_scopeFactory);
             await dualWrite.EnqueueOnlyAsync("Create", e, e.Id.ToString());
         }
         catch (Exception ex)
         {
             System.Diagnostics.Debug.WriteLine($"Failed to enqueue expense sync: {ex.Message}");
             // Don't fail the create, sync will retry
         }
     }
     
     return ok;
 }
 catch (Exception ex) { LastError = SanitizeError(ex); return false; }
 }

 public async Task<bool> DeleteExpenseAsync(int id)
 {
 try 
 { 
     LastError = null;
     var e = await _db.Expenses.FindAsync(id); 
     if (e == null) return false; 
     
     _db.Expenses.Remove(e); 
     var ok = await _db.SaveChangesAsync() > 0;
     
     // Sync deletion to cloud via outbox
     if (ok)
     {
         try
         {
             var dualWrite = new DualWriteService(_scopeFactory);
             await dualWrite.EnqueueOnlyAsync("Delete", e, e.Id.ToString());
         }
         catch (Exception ex)
         {
             System.Diagnostics.Debug.WriteLine($"Failed to enqueue expense delete sync: {ex.Message}");
         }
     }
     
     return ok;
 }
 catch (Exception ex) { LastError = SanitizeError(ex); return false; }
 }

 // Cash Flow (simple) ----------------------------------------
 public sealed record CashFlowSummary(
 decimal Inflows,
 decimal Outflows,
 decimal Net,
 IReadOnlyDictionary<string, decimal> InflowsByMonth,
 IReadOnlyDictionary<string, decimal> OutflowsByMonth);

 // Get all accounts for invoice dropdown
 public async Task<List<Account>> GetAccountsAsync()
 {
 try
 {
 return await _db.Accounts.OrderBy(a => a.Name).ToListAsync();
 }
 catch (Exception ex)
 {
 LastError = SanitizeError(ex);
 System.Diagnostics.Debug.WriteLine($"BillingService.GetAccountsAsync error: {ex}");
 return new List<Account>();
 }
 }

 public async Task<CashFlowSummary> GetCashFlowAsync(int months = 6)
 {
 var now = DateTime.UtcNow;
 var invoices = await _db.Invoices.AsNoTracking().ToListAsync();
 var expenses = await _db.Expenses.AsNoTracking().ToListAsync();

 // Inflows: Paid invoices AmountDue
 var inflows = invoices.Where(i => string.Equals(i.Status, "Paid", StringComparison.OrdinalIgnoreCase))
                       .Sum(i => i.AmountDue);
 // Outflows: Expenses
 var outflows = expenses.Sum(e => e.Amount);
 var net = inflows - outflows;

 var inflowByMonth = new Dictionary<string, decimal>();
 var outflowByMonth = new Dictionary<string, decimal>();
 for (int i = months - 1; i >= 0; i--)
 {
 var dt = new DateTime(now.AddMonths(-i).Year, now.AddMonths(-i).Month, 1, 0, 0, 0, DateTimeKind.Utc);
 var label = dt.ToString("yyyy-MM");
 inflowByMonth[label] = invoices.Where(inv => inv.PaidDate.HasValue && inv.PaidDate.Value.Year == dt.Year && inv.PaidDate.Value.Month == dt.Month)
                                .Sum(inv => inv.AmountDue);
 outflowByMonth[label] = expenses.Where(exp => exp.Date.Year == dt.Year && exp.Date.Month == dt.Month)
                                 .Sum(exp => exp.Amount);
 }

 return new CashFlowSummary(inflows, outflows, net, inflowByMonth, outflowByMonth);
 }

 // ==================== CLIENT/CUSTOMER BILLING INTEGRATION ====================

 /// <summary>
 /// Get all clients (customers) for invoice dropdown
 /// </summary>
 public async Task<List<Client>> GetClientsAsync()
 {
     try
     {
         return await _db.Clients.OrderBy(c => c.Name).ToListAsync();
     }
     catch (Exception ex)
     {
         LastError = SanitizeError(ex);
         System.Diagnostics.Debug.WriteLine($"BillingService.GetClientsAsync error: {ex}");
         return new List<Client>();
     }
 }

 /// <summary>
 /// Generate monthly subscription invoices for all active clients
 /// </summary>
 public async Task<int> GenerateMonthlyInvoicesAsync(DateTime billingMonth)
 {
     try
     {
         LastError = null;
         var firstOfMonth = new DateTime(billingMonth.Year, billingMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc);
         var dueDate = firstOfMonth.AddDays(30);
         
         // Get active clients with plans
         var clients = await _db.Clients
             .Where(c => c.Status == "Active" && !string.IsNullOrWhiteSpace(c.Plan))
             .ToListAsync();

         // Check for existing invoices this month
         var existingInvoices = await _db.Invoices
             .Where(i => i.IssueDate.Year == firstOfMonth.Year && 
                        i.IssueDate.Month == firstOfMonth.Month &&
                        i.InvoiceType == "Subscription")
             .Select(i => i.ClientId)
             .ToListAsync();

         int created = 0;
         foreach (var client in clients)
         {
             // Skip if already invoiced this month
             if (existingInvoices.Contains(client.Id)) continue;

             // Calculate amount based on plan
             var amount = GetPlanAmount(client.Plan);
             if (amount <= 0) continue;

             var invoice = new Invoice
             {
                 ClientId = client.Id,
                 ClientName = client.Name,
                 AccountId = client.AccountId,
                 AmountDue = amount,
                 IssueDate = firstOfMonth,
                 DueDate = dueDate,
                 Status = "Unpaid",
                 InvoiceType = "Subscription",
                 Notes = $"Monthly subscription - {client.Plan} plan for {firstOfMonth:MMMM yyyy}"
             };

             // Fill account name if available
             if (client.AccountId.HasValue)
             {
                 var account = await _db.Accounts.FindAsync(client.AccountId.Value);
                 if (account != null) invoice.AccountName = account.Name;
             }

             _db.Invoices.Add(invoice);
             created++;
         }

         if (created > 0)
         {
             await _db.SaveChangesAsync();
         }

         return created;
     }
     catch (Exception ex)
     {
         LastError = SanitizeError(ex);
         System.Diagnostics.Debug.WriteLine($"BillingService.GenerateMonthlyInvoicesAsync error: {ex}");
         return 0;
     }
 }

 /// <summary>
 /// Create a service invoice linked to a resolved ticket
 /// </summary>
 public async Task<bool> CreateServiceInvoiceAsync(int ticketId, decimal amount, string? notes = null)
 {
     try
     {
         LastError = null;
         
         var ticket = await _db.Tickets.FindAsync(ticketId);
         if (ticket == null) { LastError = "Ticket not found"; return false; }

         Client? client = null;
         if (!string.IsNullOrWhiteSpace(ticket.ClientId))
         {
             client = await _db.Clients.FindAsync(ticket.ClientId);
         }

         var invoice = new Invoice
         {
             ClientId = ticket.ClientId,
             ClientName = ticket.ClientName ?? client?.Name,
             AccountId = ticket.AccountId ?? client?.AccountId,
             AmountDue = amount,
             IssueDate = DateTime.UtcNow,
             DueDate = DateTime.UtcNow.AddDays(15), // Service invoices due in 15 days
             Status = "Unpaid",
             InvoiceType = "Service",
             RelatedTicketId = ticketId,
             Notes = notes ?? $"Service charge for Ticket #{ticketId}: {ticket.Title}"
         };

         // Fill account name if available
         if (invoice.AccountId.HasValue)
         {
             var account = await _db.Accounts.FindAsync(invoice.AccountId.Value);
             if (account != null) invoice.AccountName = account.Name;
         }

         _db.Invoices.Add(invoice);
         var ok = await _db.SaveChangesAsync() > 0;

         if (ok)
         {
             try
             {
                 var dualWrite = new DualWriteService(_scopeFactory);
                 await dualWrite.EnqueueOnlyAsync("Create", invoice, invoice.Id.ToString());
             }
             catch { }
         }

         return ok;
     }
     catch (Exception ex)
     {
         LastError = SanitizeError(ex);
         System.Diagnostics.Debug.WriteLine($"BillingService.CreateServiceInvoiceAsync error: {ex}");
         return false;
     }
 }

 /// <summary>
 /// Get invoices for a specific client
 /// </summary>
 public async Task<List<Invoice>> GetClientInvoicesAsync(string clientId)
 {
     try
     {
         if (string.IsNullOrWhiteSpace(clientId)) return new List<Invoice>();
         return await _db.Invoices
             .Where(i => i.ClientId == clientId)
             .OrderByDescending(i => i.IssueDate)
             .ToListAsync();
     }
     catch (Exception ex)
     {
         LastError = SanitizeError(ex);
         return new List<Invoice>();
     }
 }

 /// <summary>
 /// Get unpaid invoices for a client
 /// </summary>
 public async Task<List<Invoice>> GetUnpaidInvoicesAsync(string? clientId = null)
 {
     try
     {
         var query = _db.Invoices
             .Where(i => i.Status != "Paid");

         if (!string.IsNullOrWhiteSpace(clientId))
             query = query.Where(i => i.ClientId == clientId);

         return await query.OrderByDescending(i => i.IssueDate).ToListAsync();
     }
     catch (Exception ex)
     {
         LastError = SanitizeError(ex);
         return new List<Invoice>();
     }
 }

 /// <summary>
 /// Get plan amount based on plan name
 /// </summary>
 private static decimal GetPlanAmount(string? plan)
 {
     // Configure your plan pricing here
     return (plan?.ToLowerInvariant()) switch
     {
         "basic" => 999.00m,
         "standard" => 1499.00m,
         "premium" => 2499.00m,
         "enterprise" => 4999.00m,
         _ => 0m // Unknown plan - don't generate invoice
     };
 }

 /// <summary>
 /// Get client billing summary
 /// </summary>
 public async Task<ClientBillingSummary> GetClientBillingSummaryAsync(string clientId)
 {
     try
     {
         if (string.IsNullOrWhiteSpace(clientId))
             return new ClientBillingSummary(0, 0, 0, 0, 0);

         var invoices = await _db.Invoices
             .Where(i => i.ClientId == clientId)
             .ToListAsync();

         var total = invoices.Sum(i => i.AmountDue);
         var paid = invoices.Where(i => i.Status == "Paid").Sum(i => i.AmountDue);
         var unpaid = invoices.Where(i => i.Status != "Paid").Sum(i => i.AmountDue);
         var overdue = invoices.Where(i => i.Status != "Paid" && 
                                           (i.DueDate ?? i.IssueDate.AddDays(30)) < DateTime.UtcNow)
                               .Sum(i => i.AmountDue);
         var invoiceCount = invoices.Count;

         return new ClientBillingSummary(total, paid, unpaid, overdue, invoiceCount);
     }
     catch
     {
         return new ClientBillingSummary(0, 0, 0, 0, 0);
     }
 }

 public sealed record ClientBillingSummary(
     decimal TotalBilled,
     decimal TotalPaid,
     decimal TotalUnpaid,
     decimal TotalOverdue,
     int InvoiceCount);
 }
}
