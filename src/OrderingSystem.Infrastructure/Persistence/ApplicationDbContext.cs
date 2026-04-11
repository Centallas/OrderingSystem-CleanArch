using Microsoft.EntityFrameworkCore;
using OrderingSystem.Application.Abstractions.Data; // Add this
using OrderingSystem.Domain.Entities;

namespace OrderingSystem.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
    : DbContext(options), IApplicationDbContext 
{
    public DbSet<Order> Orders { get; set; }

    // This ensures the interface's SaveChangesAsync is satisfied
    public override Task<Int32> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }
}