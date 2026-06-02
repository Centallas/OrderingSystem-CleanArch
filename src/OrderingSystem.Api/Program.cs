using System;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderingSystem.Application.Orders.Commands.CreateOrder;
using OrderingSystem.Domain.Repositories;
using OrderingSystem.Infrastructure.Persistence;
using OrderingSystem.Infrastructure.Persistence.Repositories;
using OrderingSystem.Application.Abstractions.Data;
using OrderingSystem.Application;
using OrderingSystem.Infrastructure.Exceptions;
using OrderingSystem.Infrastructure.Messaging;
using OrderingSystem.API.Consumers;
using OrderingSystem.Shared; // Added to access the OrderAnalysisCompleted interface

var builder = WebApplication.CreateBuilder(args);

// --- Standard Services ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- Database Configuration ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// --- Application Logic & MediatR ---
builder.Services.AddApplication();
builder.Services.AddExceptionHandler<CustomExceptionHandler>();
builder.Services.AddProblemDetails();

// --- MassTransit Setup ---
builder.Services.AddMassTransit(x =>
{
    // Register the consumer
    x.AddConsumer<OrderAnalysisCompletedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitConfig = builder.Configuration.GetSection("MessageBroker");
        var host = rabbitConfig["Host"] ?? "localhost";
        
        // Read the port parameter dynamically, falling back to 5672 if missing
        ushort port = ushort.TryParse(rabbitConfig["Port"], out var p) ? p : (ushort)5672;

        cfg.Host(host, port, "/", h =>
        {
            h.Username(rabbitConfig["Username"] ?? "guest");
            h.Password(rabbitConfig["Password"] ?? "guest");
        });

        cfg.Message<OrderAnalysisCompleted>(m => m.SetEntityName("order-analysis-completed"));

        cfg.ReceiveEndpoint("order-analysis-completed-queue", e =>
        {
            e.ConfigureConsumer<OrderAnalysisCompletedConsumer>(context);
        });

        cfg.ConfigureEndpoints(context);
    });
});

// This ensures the bus starts and stops with the web application
builder.Services.AddOptions<MassTransitHostOptions>()
    .Configure(options =>
    {
        options.WaitUntilStarted = true;
    });

// Add Redis Caching
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "OrderingSystem_";
});

var app = builder.Build();

// --- Middleware Pipeline ---
app.UseExceptionHandler();

// Enable Swagger for all environments (including Production in Docker)
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    options.RoutePrefix = string.Empty; // This makes Swagger the home page
});

//app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// 1. Fixed: Use the non-blocking async startup method
await app.RunAsync();

// --- Namespaced Types Section ---
namespace OrderingSystem.Api
{
    // --- Helper Class for Basic Tier ---
    // This prevents MassTransit from trying to create Service Bus Topics
    public class BasicTierEntityNameFormatter : IEntityNameFormatter
    {
        public string FormatEntityName<T>() => typeof(T).Name;
    }
}

// --- Global Types Section ---
// Keeping this outside of any namespace ensures it maps perfectly to your WebApplicationFactory integration tests!
public partial class Program { }