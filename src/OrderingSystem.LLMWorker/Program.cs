using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using MassTransit;
using OrderingSystem.LLMWorker;
using OrderingSystem.Shared;
using Microsoft.EntityFrameworkCore;
using OrderingSystem.Infrastructure.Persistence;

var builder = Host.CreateApplicationBuilder(args);

// 1. Database and HTTP Setup
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
var ollamaEndpoint = builder.Configuration["Ollama:Endpoint"] ?? "http://localhost:11434/v1/";

// 2. Semantic Kernel (Ollama) Setup
builder.Services.AddKernel()
    .AddOpenAIChatCompletion(
        modelId: "llama3",
        apiKey: "unused",
        endpoint: new Uri(ollamaEndpoint),
        httpClient: httpClient
    );

// 3. MassTransit Setup (Replacing the manual Worker)
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderCreatedConsumer>();
    x.SetKebabCaseEndpointNameFormatter();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitConfig = builder.Configuration.GetSection("MessageBroker");
        var host = rabbitConfig["Host"] ?? "localhost";

        Console.WriteLine($"---> WORKER ATTEMPTING TO CONNECT TO RABBIT AT: {host}");

        cfg.Host(host, "/", h =>
        {
            h.Username(rabbitConfig["Username"] ?? "guest");
            h.Password(rabbitConfig["Password"] ?? "guest");
        });

        // Specific configuration for the AI consumer queue
        cfg.ReceiveEndpoint("order-created", e =>
        {
            // Only take one message at a time so Ollama doesn't crash your PC
            e.PrefetchCount = 1;

            // This ensures the automatic configuration for the consumer is applied here
            e.ConfigureConsumer<OrderCreatedConsumer>(context);
        });

        // This handles any other consumers you might add later automatically
        cfg.ConfigureEndpoints(context);
    });
});
builder.Services.AddOptions<MassTransitHostOptions>()
    .Configure(options =>
    {
        options.WaitUntilStarted = false; // Changed from true
        options.StartTimeout = TimeSpan.FromSeconds(30);
    });

// REMOVED: builder.Services.AddHostedService<Worker>(); 
// MassTransit handles the background listening automatically now!

var host = builder.Build();
await host.RunAsync();