using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using OrderingSystem.Domain.Entities;
using OrderingSystem.Domain.Repositories;
using System.Text.Json;

namespace OrderingSystem.Infrastructure.Persistence.Repositories;

public class OrderRepository(ApplicationDbContext context, IDistributedCache cache) : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(Guid id)
    {
        string cacheKey = $"order:{id}";

        // Use these options for both Serialize and Deserialize
        var serializerOptions = new JsonSerializerOptions
        {
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
            PropertyNameCaseInsensitive = true
        };

        var cachedOrder = await cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedOrder))
        {
            try
            {
                var result = JsonSerializer.Deserialize<Order>(cachedOrder, serializerOptions);
                Console.WriteLine("-----> SUCCESSFUL REDIS RETRIEVAL!");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"-----> REDIS DESERIALIZATION ERROR: {ex.Message}");
            }
        }

        Console.WriteLine("-----> REDIS MISS: Fetching from DB...");
        var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == id);

        if (order != null)
        {
            Console.WriteLine("-----> SAVING TO REDIS...");
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };

            var serializedOrder = JsonSerializer.Serialize(order, serializerOptions);
            await cache.SetStringAsync(cacheKey, serializedOrder, options);
        }

        return order;
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

        // Invalidate cache on update so we don't serve "stale" data
        await cache.RemoveAsync($"order:{order.Id}");

        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var order = await GetByIdAsync(id);
        if (order != null)
        {
            context.Orders.Remove(order);

            // Remove from cache if deleted
            await cache.RemoveAsync($"order:{id}");

            await context.SaveChangesAsync();
        }
    }
}