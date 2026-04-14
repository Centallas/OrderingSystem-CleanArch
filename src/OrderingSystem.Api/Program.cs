using Microsoft.EntityFrameworkCore;
using OrderingSystem.Application.Orders.Commands.CreateOrder;
using OrderingSystem.Domain.Repositories;
using OrderingSystem.Infrastructure.Persistence;
using OrderingSystem.Infrastructure.Persistence.Repositories;
using OrderingSystem.Application.Abstractions.Data;
using OrderingSystem.Application;

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
//builder.Services.AddMediatR(cfg => 
  //  cfg.RegisterServicesFromAssembly(typeof(CreateOrderCommand).Assembly));
builder.Services.AddApplication();
// Add the Global Exception Handler
builder.Services.AddExceptionHandler<OrderingSystem.Infrastructure.Exceptions.CustomExceptionHandler>();
// This is required to generate the standardized ProblemDetails response
builder.Services.AddProblemDetails();

var app = builder.Build();

// Use the exception handling middleware
app.UseExceptionHandler();

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
