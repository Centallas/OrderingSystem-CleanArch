using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderingSystem.Application.Abstractions.Data;


namespace OrderingSystem.Application.Orders.Queries.GetOrders;

public class GetOrdersQueryHandler(IApplicationDbContext context) 
    : IRequestHandler<GetOrdersQuery, List<OrderResponse>>
{
    public async Task<List<OrderResponse>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        // This goes to the real PostgreSQL DB and maps the result
        return await context.Orders
            .Select(o => new OrderResponse(o.Id, o.CustomerName, o.TotalAmount))
            .ToListAsync(cancellationToken);
    }
}