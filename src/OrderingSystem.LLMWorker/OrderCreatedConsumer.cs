using MassTransit;
using Microsoft.Extensions.Logging;
using OrderingSystem.Shared;
using Microsoft.SemanticKernel;
using OrderingSystem.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using OrderingSystem.Domain.Entities;

namespace OrderingSystem.LLMWorker;

public class OrderCreatedConsumer : IConsumer<OrderCreated>
{
    private readonly ILogger<OrderCreatedConsumer> _logger;
    private readonly Kernel _kernel;
    private readonly IServiceScopeFactory _scopeFactory;

    public OrderCreatedConsumer(
        ILogger<OrderCreatedConsumer> logger,
        Kernel kernel,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _kernel = kernel;
        _scopeFactory = scopeFactory;
    }

    public async Task Consume(ConsumeContext<OrderCreated> context)
    {
        var message = context.Message;
        _logger.LogInformation("[LLMWorker] Processing Order: {Id} for {Customer}", message.OrderId, message.CustomerName);

        try
        {
            // 1. Build the prompt for the LLM
            var itemsSummary = string.Join(", ", message.Items.Select(i => $"{i.Quantity}x {i.Product}"));
            //var prompt = $"Analyze this order for {message.CustomerName}: {itemsSummary}. Provide a short 1-sentence summary.";
            var prompt = $@"Analyze this order for {message.CustomerName}: {itemsSummary}. 
                            Provide ONLY a 1-sentence summary of the customer's intent. 
                            Do not include headers, bullets, or introductory text.";

            // 2. Call the LLM (Ollama)
            var result = await _kernel.InvokePromptAsync(prompt);
            var aiAnalysis = result.ToString();

            _logger.LogInformation("[LLMWorker] AI Analysis: {Analysis}", aiAnalysis);

            // 3. Save to Database using a Scoped Context
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var order = await dbContext.Orders.FindAsync(message.OrderId);

                if (order != null)
                {
                    // 1. Assign the LLM result to the column/property
                    order.SetAISummary(aiAnalysis);

                    // 2. Now SaveChanges will see the 'AISummary' property is modified
                    await dbContext.SaveChangesAsync();

                    _logger.LogInformation("[LLMWorker] Database updated for Order {Id}", message.OrderId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LLMWorker] Error processing Order {Id}", message.OrderId);
            throw; // MassTransit will handle retries if we throw
        }
    }
}