using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly Kernel _kernel;
    private IConnection? _connection;
    private IChannel? _channel;

    public Worker(ILogger<Worker> logger, Kernel kernel)
    {
        _logger = logger;
        _kernel = kernel;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LLM Worker starting...");

        // 1. Initialize Connection Asynchronously
        var factory = new ConnectionFactory() { HostName = "localhost" };
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

            var result = await _kernel.InvokePromptAsync($"Analyze this order: {message}");
            
            _logger.LogInformation(" [x] LLM Analysis: {result}", result);
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