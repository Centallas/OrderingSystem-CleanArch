namespace OrderingSystem.Domain.Entities;

public class Order
{
    public required Guid Id { get; init; }
    public required string CustomerName { get; init; }
    public required DateTime OrderDate { get; init; }
    public required decimal TotalAmount { get; init; }
    public List<string> OrderItems { get; init; } = [];
}
