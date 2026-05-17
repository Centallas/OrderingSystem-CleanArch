using MediatR;


namespace OrderingSystem.Application.Orders.Commands.CreateOrder;

// The 'record' must be inside the namespace, but outside of other members
public record CreateOrderCommand(
    string CustomerName, 
    List<OrderItemCommandDto> Items) : IRequest<Guid>;

public record OrderItemCommandDto(string Product, decimal Price, int Quantity);