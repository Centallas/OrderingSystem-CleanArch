using MediatR;
using OrderingSystem.Domain.Entities;
using OrderingSystem.Domain.Repositories;

namespace OrderingSystem.Application.Orders.Commands.CreateOrder;

public class CreateOrderCommandHandler(IOrderRepository orderRepository) : IRequestHandler<CreateOrderCommand, Guid>
{
    public async Task<Guid> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerName = command.CustomerName,
            OrderDate = DateTime.UtcNow,
            TotalAmount = command.TotalAmount
        };

        await orderRepository.AddAsync(order);

        return order.Id;
    }
}
