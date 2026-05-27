using System;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderingSystem.Application.Orders.Commands.CreateOrder;
using OrderingSystem.Domain.Repositories;
using OrderingSystem.Infrastructure.Persistence;
using OrderingSystem.Infrastructure.Persistence.Repositories;
using OrderingSystem.Application.Abstractions.Data;
using OrderingSystem.Application;
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
builder.Services.AddExceptionHandler<OrderingSystem.Infrastructure.Exceptions.CustomExceptionHandler>();
builder.Services.AddProblemDetails();

// --- MassTransit Setup ---
builder.Services.AddMassTransit(x =>
{
    // Register the consumer
    x.AddConsumer<OrderAnalysisCompletedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitConfig = builder.Configuration.GetSection("MessageBroker");
        cfg.Host(rabbitConfig["Host"], "/", h =>
        {
            h.Username(rabbitConfig["Username"]);
            h.Password(rabbitConfig["Password"]);
        });

        // 1. Explicitly map the outbound/inbound exchange contract topology name
        cfg.Message<OrderAnalysisCompleted>(m => m.SetEntityName("order-analysis-completed"));

        // 2. Configure a dedicated receive endpoint queue to break the name collision
        cfg.ReceiveEndpoint("order-analysis-completed-queue", e =>
        {
            // Bind this consumer directly to this queue
            e.ConfigureConsumer<OrderAnalysisCompletedConsumer>(context);
        });

        // Configure remaining endpoints automatically if any exist
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

app.Run();

// --- Helper Class for Basic Tier ---
// This prevents MassTransit from trying to create Service Bus Topics
public class BasicTierEntityNameFormatter : IEntityNameFormatter
{
    public string FormatEntityName<T>() => typeof(T).Name;
}