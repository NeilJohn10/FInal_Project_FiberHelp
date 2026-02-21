using System;

namespace FiberHelp.Models
{
    public class Expense
    {
        public int Id { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public string Category { get; set; } = "General";
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Reference { get; set; }
    }
}
