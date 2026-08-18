using LatamPriceChecker.Models;
using Microsoft.EntityFrameworkCore;

namespace LatamPriceChecker.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<MonitoredItem> MonitoredItems => Set<MonitoredItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MonitoredItem>(entity =>
            {
                entity.ToTable("monitored_items");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("id");

                entity.Property(e => e.SearchWord)
                    .HasColumnName("search_word")
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.TargetPrice)
                    .HasColumnName("target_price")
                    .IsRequired();

                entity.HasIndex(e => e.SearchWord);
            });
        }
    }
}
