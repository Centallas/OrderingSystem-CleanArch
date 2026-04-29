using MassTransit;
// Replace 'Ordering.Contracts' with the actual namespace where your OrderCreated class lives
// It's likely in OrderingSystem.Domain or a shared Contracts project
using OrderingSystem.Domain.Events; 

namespace OrderingSystem.Api.Consumers
{
    public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
    {
        public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
        {
            var data = context.Message;
            
            // This is the "eating" part!
            Console.WriteLine("-------------------------------------------------------");
            Console.WriteLine($"[CONSUMER] Success! Message received from RabbitMQ.");
            Console.WriteLine($"[CONSUMER] Order ID: {data.OrderId}");
            Console.WriteLine($"[CONSUMER] Customer: {data.CustomerName}");
            Console.WriteLine($"[CONSUMER] Total: ${data.TotalAmount}");
            Console.WriteLine("-------------------------------------------------------");

            await Task.CompletedTask;
        }
    }
}