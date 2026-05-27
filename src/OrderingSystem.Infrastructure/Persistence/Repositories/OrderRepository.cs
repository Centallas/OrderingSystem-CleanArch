using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using OrderingSystem.Domain.Entities;
using OrderingSystem.Domain.Repositories;
using System.Text.Json;

namespace OrderingSystem.Infrastructure.Persistence.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly bool _isRedisEnabled;

    public OrderRepository(
        ApplicationDbContext context, 
        IDistributedCache cache,
        IConfiguration configuration)
    {
        _context = context;
        _cache = cache;
    
        // .NET automatically resolves hierarchical structures from RedisSettings__IsEnabled
        _isRedisEnabled = configuration.GetValue<bool>("RedisSettings:IsEnabled");
        
        Console.WriteLine($"-----> [OrderRepository] Caching Active: {_isRedisEnabled}");
    }

    public async Task<Order?> GetByIdAsync(Guid id)
    {
        if (!_isRedisEnabled)
        {
            return await FetchFromDbAsync(id);
        }

        string cacheKey = $"order:{id}";
        var serializerOptions = new JsonSerializerOptions
        {
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
            PropertyNameCaseInsensitive = true,
            IncludeFields = true 
        };

        try
        {
            var cachedOrder = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedOrder))
            {
                var result = JsonSerializer.Deserialize<Order>(cachedOrder, serializerOptions);
                if (result != null && result.Id == Guid.Empty)
                {
                    typeof(Order).GetProperty(nameof(Order.Id))?.SetValue(result, id, null);
                }
                return result;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"-----> REDIS READ ERROR (Bypassing): {ex.Message}");
        }

        return await FetchFromDbAsync(id);
    }

    public async Task<IEnumerable<Order>> GetAllAsync()
    {
        return await _context.Orders
            .Include(o => o.Items)
            .ToListAsync();
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await _context.Orders.AddAsync(order, cancellationToken);
    }

    public async Task UpdateAsync(Order order)
    {
        var local = _context.Orders
            .Local
            .FirstOrDefault(entry => entry.Id == order.Id);

        if (local != null)
        {
            _context.Entry(local).State = EntityState.Detached;
        }

        _context.Orders.Update(order);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order != null)
        {
            _context.Orders.Remove(order);
        }
    }

    private async Task<Order?> FetchFromDbAsync(Guid id)
    {
        Console.WriteLine("-----> FETCHING FROM DB...");
        return await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);
    }
}