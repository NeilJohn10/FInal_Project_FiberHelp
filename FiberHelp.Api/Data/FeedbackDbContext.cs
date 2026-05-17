using Microsoft.EntityFrameworkCore;
using FiberHelp.Api.Models;

namespace FiberHelp.Api.Data
{
    public class FeedbackDbContext : DbContext
    {
        public FeedbackDbContext(DbContextOptions<FeedbackDbContext> options) : base(options) { }

        public DbSet<ClientFeedback> ClientFeedbacks { get; set; }
    }
}
