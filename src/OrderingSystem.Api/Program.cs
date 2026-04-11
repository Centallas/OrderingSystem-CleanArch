using Microsoft.EntityFrameworkCore;
using OrderingSystem.Application.Orders.Commands.CreateOrder;
using OrderingSystem.Domain.Repositories;
using OrderingSystem.Infrastructure.Persistence;
using OrderingSystem.Infrastructure.Persistence.Repositories;
using OrderingSystem.Application.Abstractions.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register ApplicationDbContext with PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                      ?? "Host=localhost;Database=OrderingDb;Username=postgres;Password=password";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

// Register Repository
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// Register MediatR
builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(typeof(CreateOrderCommand).Assembly));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
