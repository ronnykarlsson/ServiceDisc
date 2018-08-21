using System;
using System.Threading.Tasks;
using ServiceDisc.Models;
using ServiceDisc.Networking.QueueService;
using ServiceDisc.Networking.ServiceDiscConnection;
using Xunit;

namespace ServiceDisc.Tests.Networking.ServiceDiscConnection
{
    public class ConnectionStringParserTests
    {
        public class CreateServiceDiscMethod
        {
            [Fact]
            public void ParseNullConnectionString()
            {
                Assert.Throws<ArgumentException>(() => ConnectionStringParser.Create(null));
            }

            [Fact]
            public void ParseEmptyConnectionString()
            {
                Assert.Throws<ArgumentException>(() => ConnectionStringParser.Create(""));
            }

            [Fact]
            public void CreateServiceDiscConnectionFromNamedConnectionString()
            {
                Environment.SetEnvironmentVariable("SomeServiceDiscConnectionString", "ProviderName=InMemoryServiceDiscConnection");

                var serviceDiscConnection = ConnectionStringParser.Create("SomeServiceDiscConnectionString");

                Assert.IsType<InMemoryServiceDiscConnection>(serviceDiscConnection);
            }

            [Fact]
            public void CreateServiceDiscConnectionFromNamedConnectionStringWhichDoesNotExist()
            {
                Assert.Throws<ServiceDiscException>(() => ConnectionStringParser.Create("SomeNonExistingServiceDiscConnectionString"));
            }

            [Fact]
            public void CreateServiceDiscClientFromConnectionStringWithInvalidProvider()
            {
                Environment.SetEnvironmentVariable("ServiceDiscConnectionString", "ProviderName=ThisProviderDoesNotExist");

                Assert.Throws<ServiceDiscException>(() => ConnectionStringParser.Create("ServiceDiscConnectionString"));
            }

            [Fact]
            public void CreateAzureStorageServiceDiscConnectionFromConnectionString()
            {
                Environment.SetEnvironmentVariable("ServiceDiscConnectionString", "ProviderName=ConnectionStringTestServiceDiscConnection;MyConnectionString");

                var serviceDiscConnection = ConnectionStringParser.Create("ServiceDiscConnectionString");

                Assert.IsType<ConnectionStringTestServiceDiscConnection>(serviceDiscConnection);
            }
        }
    }

    class ConnectionStringTestServiceDiscConnection : IServiceDiscConnection
    {
        private IServiceDiscConnection _serviceDiscConnection = new InMemoryServiceDiscConnection();

        public ConnectionStringTestServiceDiscConnection(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(connectionString));
        }

        public void Dispose()
        {
            _serviceDiscConnection.Dispose();
        }

        public Task RegisterAsync(ServiceInformation serviceInformation)
        {
            return _serviceDiscConnection.RegisterAsync(serviceInformation);
        }

        public Task UnregisterAsync(Guid id)
        {
            return _serviceDiscConnection.UnregisterAsync(id);
        }

        public Task<ServiceListDocument> GetServiceListAsync()
        {
            return _serviceDiscConnection.GetServiceListAsync();
        }

        public Task SendMessageAsync<T>(T message) where T : class
        {
            return _serviceDiscConnection.SendMessageAsync(message);
        }

        public Task SubscribeAsync<T>(Action<T> callback) where T : class
        {
            return _serviceDiscConnection.SubscribeAsync(callback);
        }

        public Task SendMessageAsync<T>(T message, string name) where T : class
        {
            return _serviceDiscConnection.SendMessageAsync(message, name);
        }

        public Task SubscribeAsync<T>(Action<T> callback, string name) where T : class
        {
            return _serviceDiscConnection.SubscribeAsync(callback, name);
        }
    }
}
