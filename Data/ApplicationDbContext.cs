using Microsoft.EntityFrameworkCore;
using HandyBackend.Models;

namespace HandyBackend.Data;

// Here we're setting up the ORM for the database.
// This is so we don't have to write raw SQL queries to the database.
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products { get; set; } // Create a mapping between the Product class and the products table in the database.

    // This is similar to a migration.
    // The ModelBuilder is used to configure the entities and the relationships between them.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // The base class OnModelCreating method is called to ensure that the base configuration is applied.
        base.OnModelCreating(modelBuilder);
        
        // Configure your entities here
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id); // Set the primary key for the Product entity.
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => e.Name).IsUnique();
        });
    }
}
