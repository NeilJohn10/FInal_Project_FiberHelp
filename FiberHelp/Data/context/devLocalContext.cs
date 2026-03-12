using Microsoft.EntityFrameworkCore;
using FiberHelp.Models;

namespace FiberHelp.Data.context
{
    // Developer machine local SQL Server context (localdb) used when online DB unreachable.
    // Uses the same table names defined in AppDbContext (Agents, Clients, etc.).
    // If apply_data_masking.sql has been run, re-add _Data suffix mappings here
    // so EF Core bypasses the masked views and reads/writes real data directly.
    public class devLocalContext : AppDbContext
    {
        public devLocalContext(DbContextOptions<devLocalContext> options) : base(options) { }
    }
}
