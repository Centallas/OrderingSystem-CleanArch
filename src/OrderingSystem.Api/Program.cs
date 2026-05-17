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

// In the API, we only need to tell MassTransit where RabbitMQ is.
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitConfig = builder.Configuration.GetSection("MessageBroker");
        cfg.Host(rabbitConfig["Host"], "/", h =>
        {
            h.Username(rabbitConfig["Username"]);
            h.Password(rabbitConfig["Password"]);
        });
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
    options.RoutePrefix = string.Empty; // This makes Swagger the home page (http://localhost:5000/)
});

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