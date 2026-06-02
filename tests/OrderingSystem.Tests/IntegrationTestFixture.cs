using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;
using Xunit;
using OrderingSystem.LLMWorker; 

namespace OrderingSystem.Tests
{
    public class IntegrationTestFixture : IAsyncLifetime
    {
        private IHost? _workerHost;

        public PostgreSqlContainer PostgresContainer { get; } = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("ordering_dev_test")
            .WithUsername("postgres")
            .WithPassword("postgres_test_password")
            .Build();

        public RabbitMqContainer RabbitMqContainer { get; } = new RabbitMqBuilder("rabbitmq:3-management-alpine")
            .WithUsername("guest")
            .WithPassword("guest")
            .Build();

        public RedisContainer RedisContainer { get; } = new RedisBuilder("redis:7-alpine")
            .Build();

        public async Task InitializeAsync()
        {
            // 1. Start all dynamic infrastructure containers
            await Task.WhenAll(
                PostgresContainer.StartAsync(),
                RabbitMqContainer.StartAsync(),
                RedisContainer.StartAsync()
            );

            string rabbitMqHost = RabbitMqContainer.Hostname;
            ushort rabbitMqPort = RabbitMqContainer.GetMappedPublicPort(5672);

            // 2. Force Environment Variables to guarantee absolute priority over appsettings JSON files
            Environment.SetEnvironmentVariable("MessageBroker__Host", rabbitMqHost);
            Environment.SetEnvironmentVariable("MessageBroker__Port", rabbitMqPort.ToString());
            Environment.SetEnvironmentVariable("MessageBroker__Username", "guest");
            Environment.SetEnvironmentVariable("MessageBroker__Password", "guest");
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", PostgresContainer.GetConnectionString());

            // Synchronize the Redis container configuration with the environment variables
            Environment.SetEnvironmentVariable("Redis__ConnectionString", RedisContainer.GetConnectionString());
            Environment.SetEnvironmentVariable("ConnectionStrings__Redis", RedisContainer.GetConnectionString());
            
            // Map the nested path keys to resolve configuration binder dependencies
            Environment.SetEnvironmentVariable("RedisSettings__ConnectionString", RedisContainer.GetConnectionString());
            Environment.SetEnvironmentVariable("RedisSettings__IsEnabled", "true");

            Environment.SetEnvironmentVariable("MessageBroker__QueuePrefix", "integration-test-");

            // 3. Programmatically bootstrap the real Worker background service
            _workerHost = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddWorkerServices(hostContext.Configuration);

                    services.ConfigureHttpClientDefaults(builder =>
                    {
                        builder.ConfigurePrimaryHttpMessageHandler(() => new InlineMockHttpMessageHandler());
                    });
                })
                .Build();
                
            await _workerHost.StartAsync();
        }

        public async Task DisposeAsync()
        {
            if (_workerHost != null)
            {
                await _workerHost.StopAsync();
                _workerHost.Dispose();
            }

            await Task.WhenAll(
                PostgresContainer.DisposeAsync().AsTask(),
                RabbitMqContainer.DisposeAsync().AsTask(),
                RedisContainer.DisposeAsync().AsTask()
            );
        }
    }

    public class InlineMockHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            if (request.RequestUri?.PathAndQuery.Contains("completions") == true)
            {
                var sseContent = "data: {\"choices\":[{\"delta\":{\"content\":\"Mock AI analysis completed successfully.\"},\"index\":0}]}\n\ndata: [DONE]\n";
                
                var responseStream = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(sseContent, System.Text.Encoding.UTF8, "text/event-stream")
                };
                return Task.FromResult(responseStream);
            }

            var standardJson = "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"Mock AI analysis completed successfully.\"}}]}";
            var standardResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(standardJson, System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(standardResponse);
        }
    }

    [CollectionDefinition("IntegrationTestsCollection")]
    public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
    {
    }
}