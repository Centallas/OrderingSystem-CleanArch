namespace OrderingSystem.Application.Orders.Querys.GetOrders;

public sealed record OrderResponse(
    Guid Id, 
    string CustomerName, 
    decimal TotalAmount, 
    DateTime OrderDate, 
    int Status);