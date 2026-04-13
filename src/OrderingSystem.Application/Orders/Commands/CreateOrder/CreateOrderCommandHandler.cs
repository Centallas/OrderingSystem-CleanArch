using MediatR;
using OrderingSystem.Application.Abstractions.Data;
using OrderingSystem.Domain.Entities;

namespace OrderingSystem.Application.Orders.Commands.CreateOrder;

public class CreateOrderCommandHandler(IApplicationDbContext context) 
    : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly IApplicationDbContext _context = context;

    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = new Order(request.CustomerName);

        // Adding a placeholder item so the TotalAmount isn't zero
        order.AddItem("Standard Order Item", request.TotalAmount, 1);

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        return order.Id;
    }
}