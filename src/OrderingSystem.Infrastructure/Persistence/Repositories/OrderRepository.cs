using Microsoft.EntityFrameworkCore;
using OrderingSystem.Domain.Entities;
using OrderingSystem.Domain.Repositories;

namespace OrderingSystem.Infrastructure.Persistence.Repositories;

public class OrderRepository(ApplicationDbContext context) : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(Guid id)
    {
        return await context.Orders.FindAsync(id);
    }

    public async Task<IEnumerable<Order>> GetAllAsync()
    {
        return await context.Orders.ToListAsync();
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        await context.Orders.AddAsync(order);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Order order)
    {
        context.Orders.Update(order);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var order = await GetByIdAsync(id);
        if (order != null)
        {
            context.Orders.Remove(order);
            await context.SaveChangesAsync();
        }
    }
}
