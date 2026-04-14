using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderingSystem.Application.Abstractions.Data;

namespace OrderingSystem.Application.Orders.Queries.GetOrders;

internal sealed class GetOrdersQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetOrdersQuery, IReadOnlyList<OrderResponse>>
{
    private readonly IApplicationDbContext _context = context;

   public async Task<IReadOnlyList<OrderResponse>> Handle(
    GetOrdersQuery request, 
    CancellationToken cancellationToken)
{
    // 1. Fetch the entities into memory first
    var orders = await _context.Orders
        .Include(o => o.Items) // Property name, not field name
        .ToListAsync(cancellationToken);

    // 2. Map them to the Response DTO in memory
    return orders.Select(o => new OrderResponse(
            o.Id,
            o.CustomerName,
            o.TotalAmount, // This works now because the data is in memory
            o.OrderDate,
            (int)o.Status))
        .ToList();
}
}