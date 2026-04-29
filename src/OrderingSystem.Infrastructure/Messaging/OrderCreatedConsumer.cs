using MassTransit;
using OrderingSystem.Domain.Events;
using Microsoft.Extensions.Logging;

namespace OrderingSystem.Infrastructure.Messaging;

public class OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger) : IConsumer<OrderCreatedEvent>
{
    private readonly ILogger<OrderCreatedConsumer> _logger = logger;

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation("Successfully consumed OrderCreatedEvent for OrderId: {OrderId}", message.OrderId);

        // This is where you would normally trigger follow-up logic:
        // - Send an email
        // - Start a shipping process
        // - Call an external API
        
        await Task.CompletedTask;
    }
}