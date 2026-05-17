using MassTransit;
using OrderingSystem.Shared; 
using MediatR;
using OrderingSystem.Application.Abstractions.Data; 
using OrderingSystem.Domain.Entities; 

namespace OrderingSystem.Application.Orders.Commands.CreateOrder;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;


    public CreateOrderCommandHandler(IApplicationDbContext context, IPublishEndpoint publishEndpoint)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // 1. Create the Order using your constructor
        var order = new Order(request.CustomerName); 

        // 2. Add items using your Domain method
        // CRITICAL: Ensure your 'CreateOrderCommand' has a property named 'Items' 
        // If it is named 'OrderItems' or something else, change 'request.Items' below.
        foreach (var item in request.Items) 
        {
            order.AddItem(item.Product, item.Price, item.Quantity);
        }

        // 3. Persist to the database
        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        // 4. Publish the message via MassTransit
        // We use the 'order' object values because the Domain generates the ID and Date
        await _publishEndpoint.Publish(new OrderCreated
        {
            OrderId = order.Id,
            CustomerName = order.CustomerName,
            // Mapping the items to our Shared Record
            Items = order.Items.Select(i => new OrderItemDto(i.ProductName, i.Quantity)).ToList()
        }, cancellationToken);

        return order.Id;
    }
}