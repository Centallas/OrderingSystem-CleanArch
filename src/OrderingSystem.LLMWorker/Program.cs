using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using MassTransit;
using OrderingSystem.LLMWorker;
using Microsoft.EntityFrameworkCore;
using OrderingSystem.Infrastructure.Persistence;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Net.Http;
using OrderingSystem.Shared;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Call our new extension method to register everything cleanly using production configs
builder.Services.AddWorkerServices(builder.Configuration);

var host = builder.Build();
await host.RunAsync();

namespace OrderingSystem.LLMWorker
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWorkerServices(this IServiceCollection services, IConfiguration configuration)
        {
            // 1. Database
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection") 
                    ?? configuration["ConnectionStrings:Postgres"])); // Support both fallback naming styles

            // 2. Named HTTP Client Configuration
            services.AddHttpClient("OllamaClient", client =>
            {
                client.Timeout = TimeSpan.FromMinutes(15);
            });

            // 3. Semantic Kernel Configuration
            services.AddSingleton<IChatCompletionService>(sp =>
            {
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var client = httpClientFactory.CreateClient("OllamaClient");

                var endpointUri = new Uri(configuration["Ollama:Endpoint"] ?? "http://localhost:11434/v1/");
                var model = configuration["Ollama:Model"] ?? "llama3";

                var transport = new System.ClientModel.Primitives.HttpClientPipelineTransport(client);
                var clientOptions = new OpenAI.OpenAIClientOptions 
                { 
                    Transport = transport, 
                    NetworkTimeout = TimeSpan.FromMinutes(15),
                    Endpoint = endpointUri 
                }; 

                var openAIClient = new OpenAI.OpenAIClient(new System.ClientModel.ApiKeyCredential("ollama"), clientOptions);

                return new Microsoft.SemanticKernel.Connectors.OpenAI.OpenAIChatCompletionService( 
                    modelId: model, 
                    openAIClient: openAIClient 
                ); 
            });

            services.AddKernel();

            // 4. MassTransit Setup
            services.AddMassTransit(x =>
            {
                x.AddConsumer<OrderCreatedConsumer>(configurator => configurator.UseConcurrentMessageLimit(1));
                
                // Read the queue prefix from configuration (will default to empty string in production environments)
                var queuePrefix = configuration["MessageBroker:QueuePrefix"] ?? "";
                
                // Explicitly use SetEndpointNameFormatter with a new instance to bypass extension limits
                x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(prefix: queuePrefix, includeNamespace: false));
                
                x.UsingRabbitMq((context, cfg) =>
                {
                    var host = configuration["MessageBroker:Host"] ?? "localhost";
                    var username = configuration["MessageBroker:Username"] ?? "guest";
                    var password = configuration["MessageBroker:Password"] ?? "guest";
                    
                    // Read the port dynamically, falling back to standard 5672 if not provided
                    ushort port = ushort.TryParse(configuration["MessageBroker:Port"], out var p) ? p : (ushort)5672;
                    
                    cfg.Host(host, port, "/", h => // Pass the port here
                    {
                        h.Username(username);
                        h.Password(password);
                    });

                    cfg.Message<OrderAnalysisCompleted>(m => m.SetEntityName("order-analysis-completed"));

                    // Explicitly use the prefix on hardcoded endpoints to align with test host configuration updates
                    var endpointName = $"{queuePrefix}order-created";
                    cfg.ReceiveEndpoint(endpointName, e =>
                    {
                        e.UseTimeout(t => t.Timeout = TimeSpan.FromMinutes(15));
                        e.ConfigureConsumer<OrderCreatedConsumer>(context);
                    });
                });
            });

            return services;
        }
    }
}