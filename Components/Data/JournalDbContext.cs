using Microsoft.EntityFrameworkCore;
using JournalApp.Models;

namespace JournalApp.Data
{
    public class JournalDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<JournalEntry> JournalEntries { get; set; }

    public JournalDbContext(DbContextOptions<JournalDbContext> options)
        : base(options)
    {
    }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Optional: make sure only one entry per day
            modelBuilder.Entity<JournalEntry>()
                .HasIndex(e => e.Date)
                .IsUnique();
        }
    }
}
