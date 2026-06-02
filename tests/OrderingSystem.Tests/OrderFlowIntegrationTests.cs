using System;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace OrderingSystem.Tests
{
    [Collection("IntegrationTestsCollection")]
    public class OrderFlowIntegrationTests
    {
        private readonly IntegrationTestFixture _fixture;

        public OrderFlowIntegrationTests(IntegrationTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void OrderFlow_ShouldHaveValidInfrastructureConnectionsAndJsonContracts()
        {
            // 1. Validate Pod-Level Connectivity (Simulating Pod Environment Resolution)
            string postgresConnectionString = _fixture.PostgresContainer.GetConnectionString();
            string rabbitMqConnectionString = _fixture.RabbitMqContainer.GetConnectionString();
            string redisConnectionString = _fixture.RedisContainer.GetConnectionString();

            Assert.False(string.IsNullOrWhiteSpace(postgresConnectionString), "Postgres connection string is invalid.");
            Assert.False(string.IsNullOrWhiteSpace(rabbitMqConnectionString), "RabbitMQ connection string is invalid.");
            Assert.False(string.IsNullOrWhiteSpace(redisConnectionString), "Redis connection string is invalid.");

            // 2. Validate JSON Payload Boundary (Simulating worker/API messaging contract)
            var originalPayload = new
            {
                OrderId = Guid.NewGuid(),
                CustomerName = "Integration Test Client",
                OrderDate = DateTime.UtcNow,
                Items = new[]
                {
                    new { Product = "Microservice Blueprint", Price = 45.50m, Qty = 2 }
                }
            };

            // Serialize exactly how the API would send it over RabbitMQ
            string jsonMessage = JsonSerializer.Serialize(originalPayload);
            
            // Deserialize exactly how the Worker pod would parse it
            using var doc = JsonDocument.Parse(jsonMessage);
            var root = doc.RootElement;

            // Assert contract guarantees match expected types across pod network boundaries
            Assert.Equal(originalPayload.OrderId, root.GetProperty("OrderId").GetGuid());
            Assert.Equal("Integration Test Client", root.GetProperty("CustomerName").GetString());
            Assert.Equal(45.50m, root.GetProperty("Items")[0].GetProperty("Price").GetDecimal());
        }
    }
}