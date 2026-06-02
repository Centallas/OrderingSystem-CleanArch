using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion; 
using Microsoft.Extensions.DependencyInjection; 
using OrderingSystem.Shared;
using System.Text.Json;
using System.Text;
using OrderingSystem.Infrastructure.Persistence; // Ensure access to DbContext

namespace OrderingSystem.LLMWorker;

public class OrderCreatedConsumer : IConsumer<OrderCreated>
{
    private readonly Kernel _kernel;
    private readonly ApplicationDbContext _dbContext; // Added DbContext reference
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public OrderCreatedConsumer(Kernel kernel, ApplicationDbContext dbContext, ILogger<OrderCreatedConsumer> logger)
    {
        _kernel = kernel;
        _dbContext = dbContext; // Injected DbContext cleanly via DI container
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreated> context)
    {
        _logger.LogInformation("[LLMWorker] Processing incoming OrderCreated integration event..."); 

        try
        {
            var orderJson = JsonSerializer.Serialize(context.Message, new JsonSerializerOptions { WriteIndented = true });

            var prompt = $"""
            You are a system architecture assistant. Analyze the following newly created order from our system.
            Provide a concise summary, highlight any premium components like Shimano groupsets, and confirm if 
            the payload structure looks complete for downstream processing.
            
            Order Data:
            {orderJson}
            """;

            var chatService = _kernel.Services.GetRequiredService<IChatCompletionService>(); 

            var chatHistory = new ChatHistory(); 
            chatHistory.AddUserMessage(prompt); 

            _logger.LogInformation("[LLMWorker] Dispatching prompt directly to IChatCompletionService streaming pipeline...");

            var executionSettings = new Microsoft.SemanticKernel.Connectors.OpenAI.OpenAIPromptExecutionSettings
            {
                MaxTokens = 500,
                StopSequences = new[] { "<|end|>", "<|endoftext|>", "\nInput:", "Input:", "User:" }, // Prevents local Phi-3 hallucination loops
                ExtensionData = new Dictionary<string, object>
                {
                    { "num_ctx", 1024 }
                }
            };

            var stringBuilder = new StringBuilder();
            
            var streamingResponse = chatService.GetStreamingChatMessageContentsAsync(
                chatHistory, 
                executionSettings: executionSettings,
                cancellationToken: context.CancellationToken
            );
           
            Console.WriteLine("[LLMWorker] --- Live Token Stream Start ---");

            await foreach (var contentPiece in streamingResponse)
            {
                if (contentPiece.Content != null)
                {
                    stringBuilder.Append(contentPiece.Content);
                    Console.Write(contentPiece.Content);

                    // Safety Valve: Force break out if the model gets caught spinning in an infinite hallucination loop
                    if (stringBuilder.Length > 2000)
                    {
                        _logger.LogWarning("[LLMWorker] Hallucination loop guard triggered! Output exceeded 2000 characters. Truncating stream to save pipeline state.");
                        break;
                    }
                }
            }
            
            Console.WriteLine("\n[LLMWorker] --- Live Token Stream End ---\n");

            var llmResponseText = stringBuilder.ToString(); 

            _logger.LogInformation("[LLMWorker] Streaming analysis completed successfully from Ollama!");

            // ** Persist the AI Summary Directly Into PostgreSQL **
            _logger.LogInformation("[LLMWorker] Fetching order entity from database for ID: {OrderId}", context.Message.OrderId);
            var order = await _dbContext.Orders.FindAsync(new object[] { context.Message.OrderId }, context.CancellationToken);

            if (order != null)
            {
                _logger.LogInformation("[LLMWorker] Order record located. Invoking domain mutation method for AISummary...");
    
                 // Use the explicit domain method provided by the aggregate root
                order.SetAISummary(llmResponseText); 

                await _dbContext.SaveChangesAsync(context.CancellationToken);
                 _logger.LogInformation("[LLMWorker] Database entity state saved successfully to PostgreSQL!");
            }
            else
            {
                _logger.LogWarning("[LLMWorker] Order record with ID {OrderId} could not be found in database!", context.Message.OrderId);
            }

            // ** Publish the result back into RabbitMQ via MassTransit **
            _logger.LogInformation("[LLMWorker] Publishing OrderAnalysisCompleted integration event for OrderId: {OrderId}", context.Message.OrderId);
            
            await context.Publish<OrderAnalysisCompleted>(new
            {
                OrderId = context.Message.OrderId,
                AnalysisResult = llmResponseText,
                CompletedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LLMWorker] Error processing incoming OrderCreated message payload."); 
            throw;
        }
    }
}