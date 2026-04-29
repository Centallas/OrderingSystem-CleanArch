using MassTransit;
using MediatR;
using OrderingSystem.Domain.Entities;
using OrderingSystem.Domain.Repositories;
using OrderingSystem.Domain.Events;

namespace OrderingSystem.Application.Orders.Commands.CreateOrder;

// Updated to use IPublishEndpoint for a more decoupled Event-Driven approach
public class CreateOrderCommandHandler(IOrderRepository orderRepository, IPublishEndpoint publishEndpoint) 
    : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly IOrderRepository _orderRepository = orderRepository;
    private readonly IPublishEndpoint _publishEndpoint = publishEndpoint;

    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // 1. Create the Domain Entity
        var order = new Order(request.CustomerName);

        // 2. Add the items
        order.AddItem("Standard Order Item", request.TotalAmount, 1);

        // 3. Persist via Repository
        await _orderRepository.AddAsync(order, cancellationToken);

        // 4. PUBLISH EVENT: Broadcasts to any subscriber (Standard/Premium Tier behavior)
        // MassTransit handles the exchange creation and routing automatically
        await _publishEndpoint.Publish(new OrderCreatedEvent
        {
            OrderId = order.Id,
            CustomerName = order.CustomerName,
            TotalAmount = order.TotalAmount,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        // 5. Return the new Order ID
        return order.Id;
    }
}