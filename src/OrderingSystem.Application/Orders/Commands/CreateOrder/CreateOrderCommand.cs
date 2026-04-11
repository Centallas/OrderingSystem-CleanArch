using MediatR;

namespace OrderingSystem.Application.Orders.Commands.CreateOrder;

public record CreateOrderCommand(string CustomerName, decimal TotalAmount) : IRequest<Guid>;
