using MediatR;

namespace OrderingSystem.Application.Orders.Queries.GetOrders;

// This defines WHAT we want (a list of orders)
public record GetOrdersQuery() : IRequest<List<OrderResponse>>;

// A simple DTO to return the data
public record OrderResponse(Guid Id, string CustomerName, decimal TotalAmount);