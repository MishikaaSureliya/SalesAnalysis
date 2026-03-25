using Microsoft.EntityFrameworkCore;
using SalesAnalysis.Models;

namespace SalesAnalysis.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Sales> Sales { get; set; }
    }
}