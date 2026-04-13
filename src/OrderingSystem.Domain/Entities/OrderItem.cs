namespace OrderingSystem.Domain.Entities;

public class OrderItem
{
    public Guid Id { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }

    // Private constructor for Entity Framework Core
    private OrderItem() { }

    public OrderItem(string productName, decimal unitPrice, int quantity)
    {
        Id = Guid.NewGuid();
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }
}