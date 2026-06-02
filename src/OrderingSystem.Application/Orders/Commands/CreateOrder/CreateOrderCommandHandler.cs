using MassTransit;
using OrderingSystem.Shared; 
using MediatR;
using OrderingSystem.Application.Abstractions.Data; 
using OrderingSystem.Domain.Entities;
using OrderingSystem.Domain.Repositories; // Import your repository interface
using Microsoft.Extensions.Logging;

namespace OrderingSystem.Application.Orders.Commands.CreateOrder;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IOrderRepository _orderRepository; // Add repository dependency
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        IApplicationDbContext context, 
        IOrderRepository orderRepository, // Inject it here
        IPublishEndpoint publishEndpoint, 
        ILogger<CreateOrderCommandHandler> logger)
    {
        _context = context;
        _orderRepository = orderRepository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = new Order(request.CustomerName); 

        foreach (var item in request.Items) 
        {
            order.AddItem(item.Product, item.Price, item.Quantity);
        }

        Console.WriteLine(">>> [____] I AM THE HANDLER IN src/OrderingSystem.Application/Orders/Commands/CreateOrder/CreateOrderCommandHandler.cs");
        
        // Route through your repository to trigger the Redis pre-populate code
        await _orderRepository.AddAsync(order, cancellationToken);
        
        // Still use the Unit of Work context pattern to commit changes tracking to Postgres
        await _context.SaveChangesAsync(cancellationToken);

        Console.WriteLine($">>> HANDLER: Attempting to publish OrderCreated for Order ID: {order.Id}");
        
        await _publishEndpoint.Publish(new OrderCreated
        {
            OrderId = order.Id,
            CustomerName = order.CustomerName,
            Items = order.Items.Select(i => new OrderItemDto(i.ProductName, i.Quantity)).ToList()
        }, cancellationToken);

        Console.WriteLine($">>> HANDLER: Successfully published.");

        return order.Id;
    }
}