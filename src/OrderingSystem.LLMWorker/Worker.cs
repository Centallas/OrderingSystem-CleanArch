using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using OrderingSystem.LLMWorker.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OrderingSystem.Infrastructure.Persistence;
using OrderingSystem.Domain.Entities;



public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly Kernel _kernel;
    private readonly IServiceScopeFactory _scopeFactory; // 1. Added this field
    private IConnection? _connection;
    private IChannel? _channel
    ;
    private IConfiguration _configuration;

    public Worker(ILogger<Worker> logger, Kernel kernel, IServiceScopeFactory scopeFactory, IConfiguration configuration)
    {
        _logger = logger;
        _kernel = kernel;
        _scopeFactory = scopeFactory;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LLM Worker starting...");

        // 1. Initialize Connection Asynchronously
        // In your Worker constructor, inject IConfiguration
        // Then in ExecuteAsync:
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:Host"] ?? "localhost"
        };
        _connection = await factory.CreateConnectionAsync(stoppingToken);

        // 2. Initialize Channel Asynchronously
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        // 3. Declare Queue
        await _channel.QueueDeclareAsync(queue: "order_processing_queue",
                                        durable: true,
                                        exclusive: false,
                                        autoDelete: false,
                                        arguments: null,
                                        cancellationToken: stoppingToken);

        // 4. Setup Consumer
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            _logger.LogInformation(" [x] Processing: {0}", message);

            // Update your existing line to this:
            var result = await _kernel.InvokePromptAsync($@"
                Analyze this order and return the result strictly in JSON format. 
                Do not include any conversational text or markdown code blocks, just the raw JSON.
                
                Input: {message}
                
                Format:
                {{
                    ""items"": [
                        {{ ""product"": ""string"", ""quantity"": 0 }}
                    ],
                    ""total"": 0.0
                }}");
            // The magic happens here:
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var order = JsonSerializer.Deserialize<OrderDto>(result.ToString(), options);
            if (order != null)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    // 1. Create the order using your domain constructor
                    var newOrder = new Order("Customer from LLM"); // You can replace this with actual customer data if available
                    // 2. Populate the items using your business method
                    foreach (var itemDto in order.Items)
                    {
                        // Note: The LLM JSON didn't include price, so we use 0.0m for now.
                        // In a real app, you would look up the actual price from a Product service.
                        newOrder.AddItem(itemDto.Product, 0.0m, itemDto.Quantity); // Price is set to 0 for now, you can enhance this later
                    }
                    dbContext.Orders.Add(newOrder);
                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation(" [x] Successfully parsed order with {count} items.", order.Items.Count);
                }

            }
        };

        // 5. Start Consuming
        await _channel.BasicConsumeAsync(queue: "order_processing_queue",
                                        autoAck: true,
                                        consumer: consumer,
                                        cancellationToken: stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}