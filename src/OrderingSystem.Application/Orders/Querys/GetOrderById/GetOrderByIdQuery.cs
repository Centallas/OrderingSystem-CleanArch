using MediatR;
using OrderingSystem.Domain.Entities;

namespace OrderingSystem.Application.Orders.Querys.GetOrderById;

public record GetOrderByIdQuery(Guid Id) : IRequest<Order?>;