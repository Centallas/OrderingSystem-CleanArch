using Microsoft.EntityFrameworkCore;
using OrderingSystem.Application.Abstractions.Data;
using OrderingSystem.Domain.Entities;

namespace OrderingSystem.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; } // New DbSet

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Order and its relationship to OrderItems
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);

            // 1. First, define the relationship
            entity.HasMany(o => o.Items)
                  .WithOne()
                  .HasForeignKey("OrderId")
                  .OnDelete(DeleteBehavior.Cascade);

            // 2. Then, tell EF to use the private field for that existing navigation
            var navigation = entity.Metadata.FindNavigation(nameof(Order.Items));
            navigation?.SetPropertyAccessMode(PropertyAccessMode.Field);
            navigation?.SetField("_items");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(oi => oi.Id);
            entity.Property(oi => oi.UnitPrice).HasPrecision(18, 2);
        });
    }
}