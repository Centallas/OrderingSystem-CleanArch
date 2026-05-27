using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using MassTransit;
using OrderingSystem.LLMWorker;
using Microsoft.EntityFrameworkCore;
using OrderingSystem.Infrastructure.Persistence;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Net.Http;
using OrderingSystem.Shared; // Added to access the OrderAnalysisCompleted interface

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// 1. Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Named HTTP Client Configuration (Extended to 15 minutes for slow CPU token generation)
builder.Services.AddHttpClient("OllamaClient", client =>
{
    client.Timeout = TimeSpan.FromMinutes(15);
});

// 3. Semantic Kernel Configuration
builder.Services.AddSingleton<IChatCompletionService>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var client = httpClientFactory.CreateClient("OllamaClient");

    var endpointUri = new Uri(builder.Configuration["Ollama:Endpoint"] ?? "http://localhost:11434/v1/");
    var model = builder.Configuration["Ollama:Model"] ?? "llama3";

    // Construct explicit transport using our DI-managed client infrastructure
    var transport = new System.ClientModel.Primitives.HttpClientPipelineTransport(client);

    // Initialize explicit options ensuring the custom local endpoint is properly mapped and timeout extended
    var clientOptions = new OpenAI.OpenAIClientOptions 
    { 
        Transport = transport, 
        NetworkTimeout = TimeSpan.FromMinutes(15), // Extended to match HttpClient limits
        Endpoint = endpointUri 
    }; 

    // Supply a generic key text string to satisfy the non-empty credential validation rules
    var openAIClient = new OpenAI.OpenAIClient(new System.ClientModel.ApiKeyCredential("ollama"), clientOptions);

    // Return the service instance bound directly to our unlocked engine client
    return new Microsoft.SemanticKernel.Connectors.OpenAI.OpenAIChatCompletionService( 
        modelId: model, 
        openAIClient: openAIClient 
    ); 
});

// Register the Kernel (it will automatically scan the container and bind the IChatCompletionService above)
builder.Services.AddKernel();

// DEBUG: Environment Variable check
var hostFromEnv = Environment.GetEnvironmentVariable("MessageBroker__Host");
Console.WriteLine($"-----> DEBUG: Environment Variable MessageBroker__Host is: '{hostFromEnv}' <-----");

// 4. MassTransit Setup
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderCreatedConsumer>(configurator => configurator.UseConcurrentMessageLimit(1));
    x.SetKebabCaseEndpointNameFormatter();
    
    x.UsingRabbitMq((context, cfg) =>
    {
        var host = builder.Configuration["MessageBroker:Host"] ?? "localhost";
        var username = builder.Configuration["MessageBroker:Username"] ?? "guest";
        var password = builder.Configuration["MessageBroker:Password"] ?? "guest";
        
        cfg.Host(host, "/", h =>
        {
            h.Username(username);
            h.Password(password);
        });

        // Explicitly map outbound contract interface publications to use the exact kebab-case exchange name
        cfg.Message<OrderAnalysisCompleted>(m => m.SetEntityName("order-analysis-completed"));

        // Explicitly configure the receive endpoint to allow 15 minutes for heavy CPU inference loops
        cfg.ReceiveEndpoint("order-created", e =>
        {
            // Configures the underlying transport pipe middleware timeout perfectly via extension method
            e.UseTimeout(t => t.Timeout = TimeSpan.FromMinutes(15));
            e.ConfigureConsumer<OrderCreatedConsumer>(context);
        });
    });
});

var host = builder.Build();
await host.RunAsync();