using OrderingSystem.Domain.Entities;

namespace OrderingSystem.Domain.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id);
    Task<IEnumerable<Order>> GetAllAsync();
    Task AddAsync(Order order, CancellationToken cancellationToken);
    Task UpdateAsync(Order order);
    Task DeleteAsync(Guid id);
}
