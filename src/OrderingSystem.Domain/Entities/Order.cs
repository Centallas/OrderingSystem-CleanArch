using OrderingSystem.Domain.Enums;

namespace OrderingSystem.Domain.Entities;

public class Order
{
    public Guid Id { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public DateTime OrderDate { get; private set; }
    public OrderStatus Status { get; private set; }
    
    // This is the collection EF will map to
    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    // Calculated property - no setter needed
    public decimal TotalAmount => _items.Sum(x => x.UnitPrice * x.Quantity);

    private Order() { } // Required for EF Core

    public Order(string customerName)
    {
        Id = Guid.NewGuid();
        CustomerName = customerName;
        OrderDate = DateTime.UtcNow;
        Status = OrderStatus.Pending;
    }

    public void AddItem(string productName, decimal price, int quantity)
    {
        _items.Add(new OrderItem(productName, price, quantity));
    }
}