using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using System.Net.Http;
using Microsoft.EntityFrameworkCore;
using OrderingSystem.Infrastructure.Persistence;

var builder = Host.CreateApplicationBuilder(args);

// Create your custom HttpClient for the timeout fix
var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Get the endpoint from configuration. 
// It will look for an Environment Variable named 'Ollama__Endpoint' 
// or a value in appsettings.json.
var ollamaEndpoint = builder.Configuration["Ollama:Endpoint"] ?? "http://localhost:11434/v1/";

builder.Services.AddKernel()
    .AddOpenAIChatCompletion(
        modelId: "llama3",
        apiKey: "unused",
        endpoint: new Uri(ollamaEndpoint), // Use the dynamic endpoint
        httpClient: httpClient
    );

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();