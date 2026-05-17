namespace OrderingSystem.Shared;

public record OrderCreated
{
    public Guid OrderId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public List<OrderItemDto> Items { get; init; } = new();
}

public record OrderItemDto(string Product, int Quantity);