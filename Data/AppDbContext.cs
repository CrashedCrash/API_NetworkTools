// API_NetworkTools/Data/AppDbContext.cs (oder Tools/Models/)
using Microsoft.EntityFrameworkCore;
using API_NetworkTools.Models; // Namespace deiner ShortUrlMapping-Klasse

namespace API_NetworkTools.Data // Oder ein passenderer Namespace
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
            // Index für ShortCode, um sicherzustellen, dass er einzigartig ist und Suchen schnell sind
            modelBuilder.Entity<ShortUrlMapping>()
                .HasIndex(s => s.ShortCode)
                .IsUnique();
        }
    }
}