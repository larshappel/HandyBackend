using HandyBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace HandyBackend.Data;

// Here we're setting up the ORM for the database.
// This is so we don't have to write raw SQL queries to the database.
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Product>? Products { get; set; } // It's nullable? Creates a mapping between the Product class and the products table in the database.

    // This is similar to a migration.
    // The ModelBuilder is used to configure the entities and the relationships between them.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // The base class OnModelCreating method is called to ensure that the base configuration is applied.
        base.OnModelCreating(modelBuilder);

        // Configure your entities here
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("orderdetails");
            entity.Property(e => e.Id).HasColumnName("OrderDetailID"); // Id column is named differently

            entity.HasKey(e => e.Id); // Set the primary key for the Product entity.

            entity.Ignore(e => e.CreatedAt); // Ignore the CreatedAt property

            entity.Property(e => e.UpdateDate).HasColumnName("UpdateDate");
            entity.Property(e => e.UpdateTime).HasColumnName("UpdateTime");

            entity.Property(e => e.OrderDetailId).IsRequired().HasColumnName("OrderDetailId");
            entity
                .Property(e => e.Amount)
                .HasColumnType("decimal(18,2)")
                .HasColumnName("SalesQuantity");
            entity.Property(e => e.IdentificationNumber).HasColumnName("IdentificationNumber");
            // and also "IdentificationNumber"
            entity.HasIndex(e => e.OrderDetailId).IsUnique();
        });
    }
}
