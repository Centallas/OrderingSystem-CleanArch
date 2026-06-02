using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore; // Added for .Migrate() extension method
using Microsoft.Extensions.DependencyInjection;
using OrderingSystem.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Xunit;

namespace OrderingSystem.Tests
{
    [Collection("IntegrationTestsCollection")]
    public class FulfillmentIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly IntegrationTestFixture _infrastructureFixture;
        private readonly HttpClient _client;

        public FulfillmentIntegrationTests(IntegrationTestFixture infrastructureFixture, WebApplicationFactory<Program> factory)
        {
            _infrastructureFixture = infrastructureFixture;
            _factory = factory;

            _client = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");

                builder.ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Error);
                });

                string rabbitMqHost = _infrastructureFixture.RabbitMqContainer.Hostname;
                ushort rabbitMqPort = _infrastructureFixture.RabbitMqContainer.GetMappedPublicPort(5672);

                builder.UseSetting("MessageBroker:Host", rabbitMqHost);
                builder.UseSetting("MessageBroker:Port", rabbitMqPort.ToString());
                builder.UseSetting("MessageBroker:Username", "guest");
                builder.UseSetting("MessageBroker:Password", "guest");

                builder.UseSetting("ConnectionStrings:DefaultConnection", _infrastructureFixture.PostgresContainer.GetConnectionString());
            }).CreateClient();

            // Force Entity Framework Core Migrations to run against the database before any test execution
            using (var scope = factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                dbContext.Database.Migrate();
            }
        }

        [Fact]
        public async Task PostOrderPayload_ShouldCompleteAllFulfillmentTasksAsynchronously()
        {
            var newOrderPayload = new
            {
                CustomerName = "Edward",
                Items = new[]
                {
                    new
                    {
                        Product = "Helmet",
                        Price = 5000,
                        Quantity = 1
                    }
                }
            };

            HttpResponseMessage response = await _client.PostAsJsonAsync("/api/orders", newOrderPayload);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"API Server Crashed with: {errorBody}");
            }         
            
            response.EnsureSuccessStatusCode(); 
            
            // Define polling constraints
            int maxAttempts = 15;
            int delayMilliseconds = 1000;
            bool isFulfilled = false;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                using (var scope = _factory.Services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    
                    var order = await dbContext.Orders
                        .FirstOrDefaultAsync(o => o.CustomerName == "Edward");

                    if (order != null && (!string.IsNullOrEmpty(order.AISummary) || (int)order.Status == 2))
                    {
                        isFulfilled = true;
                        break;
                    }
                }

                await Task.Delay(delayMilliseconds);
            }

            Assert.True(isFulfilled, "The order fulfillment pipeline timed out before the worker could complete the LLM task.");  
        }
    }
}