using MassTransit;
using Microsoft.Extensions.Logging;
using OrderingSystem.Shared;
using OrderingSystem.Domain.Repositories;

namespace OrderingSystem.API.Consumers;

public class OrderAnalysisCompletedConsumer : IConsumer<OrderAnalysisCompleted>
{
    private readonly ILogger<OrderAnalysisCompletedConsumer> _logger;
    private readonly IOrderRepository _orderRepository;

    public OrderAnalysisCompletedConsumer(
        ILogger<OrderAnalysisCompletedConsumer> logger, 
        IOrderRepository orderRepository)
    {
        _logger = logger;
        _orderRepository = orderRepository;
    }

    public async Task Consume(ConsumeContext<OrderAnalysisCompleted> context)
    {
        _logger.LogInformation("[API Consumer] Received OrderAnalysisCompleted integration event for OrderId: {OrderId}", context.Message.OrderId);
        
        var orderId = context.Message.OrderId;
        var analysisResult = context.Message.AnalysisResult;

        if (string.IsNullOrWhiteSpace(analysisResult))
        {
            _logger.LogWarning("[API Consumer] Received empty AI summary for OrderId: {OrderId}. Skipping update.", orderId);
            return;
        }

        try
        {
            // 1. Fetch the aggregate from the database
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                _logger.LogWarning("[API Consumer] Order with ID {OrderId} was not found in the database. Cannot apply AI Summary.", orderId);
                return;
            }

            // 2. Mutate the aggregate using the encapsulated domain method
            order.SetAISummary(analysisResult);

            // 3. Persist the updated aggregate state
            await _orderRepository.UpdateAsync(order);

            _logger.LogInformation("[API Consumer] Successfully persisted AISummary to PostgreSQL for OrderId: {OrderId}", orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[API Consumer] Critical error updating database for OrderId: {OrderId}", orderId);
            throw;
        }
    }
}