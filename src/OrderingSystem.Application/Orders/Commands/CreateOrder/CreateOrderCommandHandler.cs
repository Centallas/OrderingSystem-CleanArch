using MediatR;
using OrderingSystem.Domain.Entities;
using OrderingSystem.Domain.Repositories;

namespace OrderingSystem.Application.Orders.Commands.CreateOrder;

public class CreateOrderCommandHandler(IOrderRepository orderRepository) 
    : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly IOrderRepository _orderRepository = orderRepository;

    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // 1. Create the Domain Entity
        var order = new Order(request.CustomerName);

        // 2. Add the items
        order.AddItem("Standard Order Item", request.TotalAmount, 1);

        // 3. Persist via Repository
        await _orderRepository.AddAsync(order, cancellationToken);

        // 4. Return the new Order ID
        return order.Id;
    }
}