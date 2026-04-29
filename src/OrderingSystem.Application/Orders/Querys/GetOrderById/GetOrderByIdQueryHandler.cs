using MediatR;
using OrderingSystem.Domain.Entities;
using OrderingSystem.Domain.Repositories;

namespace OrderingSystem.Application.Orders.Querys.GetOrderById;

public class GetOrderByIdQueryHandler(IOrderRepository repository) 
    : IRequestHandler<GetOrderByIdQuery, Order?>
{
    public async Task<Order?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        return await repository.GetByIdAsync(request.Id);
    }
}