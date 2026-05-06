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

// CORRECT PATTERN:
// Call AddKernel(), then chain AddOpenAIChatCompletion() to the result of that.
builder.Services.AddKernel()
    .AddOpenAIChatCompletion(
        modelId: "llama3",
        apiKey: "unused",
        endpoint: new Uri("http://localhost:11434/v1/"),
        httpClient: httpClient
    );

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();