using Microsoft.EntityFrameworkCore;

namespace FiberHelp.Data.context
{
    // Developer machine local SQL Server context (localdb) used when online DB unreachable
    public class devLocalContext : AppDbContext
    {
        public devLocalContext(DbContextOptions<devLocalContext> options) : base(options) { }
    }
}
