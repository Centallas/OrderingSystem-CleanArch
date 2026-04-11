using Microsoft.EntityFrameworkCore;
using OrderingSystem.Domain.Entities; // This assumes your Order entity is in the Domain project

namespace OrderingSystem.Application.Abstractions.Data;

public interface IApplicationDbContext
{
    DbSet<Order> Orders { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}