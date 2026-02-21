using Microsoft.EntityFrameworkCore;

namespace FiberHelp.Data.context
{
    // Now used for embedded offline SQLite storage
    public class localContext : AppDbContext
    {
        public localContext(DbContextOptions<localContext> options) : base(options) { }
    }
}
