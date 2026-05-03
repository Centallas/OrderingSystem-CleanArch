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

// --- MassTransit Configuration (Fixed for Basic Tier) ---
builder.Services.AddMassTransit(x =>
{
    // 1. Tell MassTransit to look for consumers in this assembly
    x.AddConsumer<OrderCreatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitConfig = builder.Configuration.GetSection("MessageBroker");
        var host = rabbitConfig["Host"];
        var user = rabbitConfig["Username"];
        var pass = rabbitConfig["Password"];

        // This will print the length and the password in the logs so we can see if there is extra whitespace
        Console.WriteLine($"DEBUG: Attempting connection...");
        Console.WriteLine($"DEBUG: Host: '{host}'");
        Console.WriteLine($"DEBUG: User: '{user}'");
        Console.WriteLine($"DEBUG: Pass length: {pass?.Length}");
        Console.WriteLine($"DEBUG: Pass content: '{pass}'"); // This will show spaces if they exist

        cfg.Host(host, "/", h =>
        {
            h.Username(user);
            h.Password(pass);
        });
        // 2. This is crucial: it creates the receiving endpoint automatically
        cfg.ConfigureEndpoints(context);
        /*cfg.ReceiveEndpoint("order-processing-queue", e =>
        {
            e.ConfigureConsumer<OrderCreatedConsumer>(context);
        });*/
    });
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// --- Helper Class for Basic Tier ---
// This prevents MassTransit from trying to create Service Bus Topics
public class BasicTierEntityNameFormatter : IEntityNameFormatter
{
    public string FormatEntityName<T>() => typeof(T).Name;
}