using MediatR;

namespace OrderingSystem.Application.Orders.Queries.GetOrders;

// This defines WHAT we want (a list of order responses)
public record GetOrdersQuery() : IRequest<IReadOnlyList<OrderResponse>>;

// A simple DTO (Data Transfer Object) to return the data to the API
public record OrderResponse(
    Guid Id, 
    string CustomerName, 
    decimal TotalAmount, 
    DateTime OrderDate, 
    int Status);