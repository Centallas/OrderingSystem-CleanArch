using MassTransit;
using OrderingSystem.Shared; 
using MediatR;
using OrderingSystem.Application.Abstractions.Data; 
using OrderingSystem.Domain.Entities;
using Microsoft.Extensions.Logging; // ADD THIS

namespace OrderingSystem.Application.Orders.Commands.CreateOrder;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CreateOrderCommandHandler> _logger; // ADD THIS

    public CreateOrderCommandHandler(IApplicationDbContext context, IPublishEndpoint publishEndpoint, ILogger<CreateOrderCommandHandler> logger)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
        _logger = logger; // ADD THIS
    }

    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = new Order(request.CustomerName); 

        foreach (var item in request.Items) 
        {
            order.AddItem(item.Product, item.Price, item.Quantity);
        }

        // Inside Handle method:
        Console.WriteLine(">>> [!!!!] I AM THE HANDLER IN src/OrderingSystem.Application/Orders/Commands/CreateOrder/CreateOrderCommandHandler.cs");
        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        // Add this to verify we reached the Handler
        Console.WriteLine($">>> HANDLER: Attempting to publish OrderCreated for Order ID: {order.Id}");
        // Single clean Publish call
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