// API_NetworkTools/Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using API_NetworkTools.Models;

namespace API_NetworkTools.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<ShortUrlMapping> ShortUrlMappings { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<ShortUrlMapping>()
                .HasIndex(s => s.ShortCode)
                .IsUnique();
        }
    }
}