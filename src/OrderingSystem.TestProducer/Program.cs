using RabbitMQ.Client;
using System.Text;

// 1. Initialize the Factory
var factory = new ConnectionFactory() { HostName = "localhost" };

// 2. Use Async methods
using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

// 3. Declare queue asynchronously
await channel.QueueDeclareAsync(queue: "order_processing_queue",
                     durable: true,
                     exclusive: false,
                     autoDelete: false,
                     arguments: null);

string message = "Order #12345: I need 5 units of 'Super Lightweight Cycling Jersey', size M, color Blue.";
var body = Encoding.UTF8.GetBytes(message);

// 4. Publish asynchronously
await channel.BasicPublishAsync(exchange: "",
                     routingKey: "order_processing_queue",
                     body: body);

Console.WriteLine($" [x] Sent: {message}");