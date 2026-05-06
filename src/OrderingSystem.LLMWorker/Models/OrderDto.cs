namespace OrderingSystem.LLMWorker.Models;

public class OrderDto
{
    public List<OrderItemDto> Items { get; set; } = new();
    public decimal Total { get; set; }
}

public class OrderItemDto
{
    public string Product { get; set; } = string.Empty;
    public int Quantity { get; set; }
}