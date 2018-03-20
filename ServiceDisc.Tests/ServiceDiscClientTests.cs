using System;
using System.Linq;
using System.Threading;
using ServiceDisc.Networking.ServiceDiscConnection;
using Xunit;

namespace ServiceDisc.Tests
{
    public class ServiceDiscClientTests
    {
        private static IServiceDiscConnection CreateConnection()
        {
            var connection = new InMemoryServiceDiscConnection();
            return connection;
        }

        public class HostAsyncMethod
        {
            [Fact]
            public void RegistersServiceInConnection()
            {
                var connection = CreateConnection();
                var client = new ServiceDiscClient(connection);

                client.HostAsync(new TestService()).Wait();

                var service = connection.GetServiceListAsync().Result.Services.Single();
                Assert.NotNull(service);
            }

            [Fact]
            public void HostedServiceHasUniqueId()
            {
                var connection = CreateConnection();
                var client = new ServiceDiscClient(connection);

                var hostedService = client.HostAsync(new TestService()).Result;

                Assert.NotEqual(Guid.Empty, hostedService.Id);
            }

            [Fact]
            public void HostedServiceHasNoName()
            {
                var connection = CreateConnection();
                var client = new ServiceDiscClient(connection);

                var hostedService = client.HostAsync(new TestService()).Result;

                Assert.Null(hostedService.Name);
            }

            [Fact]
            public void HostedServiceWithEmptyNameHasNoName()
            {
                var connection = CreateConnection();
                var client = new ServiceDiscClient(connection);

                var hostedService = client.HostAsync(new TestService(), "").Result;

                Assert.Null(hostedService.Name);
            }

            [Fact]
            public void HostedServiceHasType()
            {
                var connection = CreateConnection();
                var client = new ServiceDiscClient(connection);

                var hostedService = client.HostAsync<ITestService>(new TestService()).Result;

                Assert.Equal(typeof(ITestService).FullName, hostedService.Type);
            }

            [Fact]
            public void HostedServiceHasUri()
            {
                var connection = CreateConnection();
                var client = new ServiceDiscClient(connection);

                var hostedService = client.HostAsync(new TestService()).Result;

                Assert.NotNull(hostedService.Address);
            }

            [Fact]
            public void HostedNamedServiceHasName()
            {
                var connection = CreateConnection();
                var client = new ServiceDiscClient(connection);

                var hostedService = client.HostAsync(new TestService(), "MyService").Result;

                Assert.Equal("MyService", hostedService.Name);
            }

            [Fact]
            public void CanHostMultipleServices()
            {
                var connection = CreateConnection();
                var client = new ServiceDiscClient(connection);

                client.HostAsync(new TestService()).Wait();
                client.HostAsync(new TestService()).Wait();
                client.HostAsync(new TestService()).Wait();

                var services = connection.GetServiceListAsync().Result.Services.Count;
                Assert.Equal(3, services);
            }

            [Fact]
            public void CanHostMultipleNamedServices()
            {
                var connection = CreateConnection();
                var client = new ServiceDiscClient(connection);

                client.HostAsync(new TestService(), "ServiceA").Wait();
                client.HostAsync(new TestService(), "ServiceB").Wait();
                client.HostAsync(new TestService(), "ServiceA").Wait();

                var serviceAs = connection.GetServiceListAsync().Result.Services.Count(s => s.Name == "ServiceA");
                var serviceBs = connection.GetServiceListAsync().Result.Services.Count(s => s.Name == "ServiceB");

                Assert.Equal(2, serviceAs);
                Assert.Equal(1, serviceBs);
            }
        }

        public class UnregisterAsyncMethod
        {
            [Fact]
            public void RemovesServiceFromConnection()
            {
                var connection = CreateConnection();
                var client = new ServiceDiscClient(connection);

                var service = client.HostAsync(new TestService()).Result;

                client.UnregisterAsync(service).Wait();
                var serviceCount = connection.GetServiceListAsync().Result.Services.Count;

                Assert.Equal(0, serviceCount);
            }

            [Fact]
            public void RemovesOneServiceFromConnection()
            {
                var connection = CreateConnection();
                var client = new ServiceDiscClient(connection);

                var service1 = client.HostAsync(new TestService()).Result;
                var service2 = client.HostAsync(new TestService()).Result;
                var service3 = client.HostAsync(new TestService()).Result;

                client.UnregisterAsync(service2).Wait();

                Assert.Equal(1, connection.GetServiceListAsync().Result.Services.Count(s => s.Id == service1.Id));
                Assert.Equal(0, connection.GetServiceListAsync().Result.Services.Count(s => s.Id == service2.Id));
                Assert.Equal(1, connection.GetServiceListAsync().Result.Services.Count(s => s.Id == service3.Id));
            }

            [Fact]
            public void RemoveNamedServiceFromConnection()
            {
                var connection = CreateConnection();
                var client = new ServiceDiscClient(connection);

                var service = client.HostAsync(new TestService(), "ServiceA").Result;

                client.UnregisterAsync(service).Wait();
                var serviceCount = connection.GetServiceListAsync().Result.Services.Count;

                Assert.Equal(0, serviceCount);
            }
        }

        public class GetAsyncMethod
        {
            [Fact]
            public void ReturnsNullIfNoServiceIsFound()
            {
                var connection = CreateConnection();
                var client = new ServiceDiscClient(connection);

                var service = client.GetAsync<TestService>().Result;

                Assert.Null(service);
            }

            [Fact]
            public void GetServiceByType()
            {
                var connection = CreateConnection();
                var client = new ServiceDiscClient(connection);

                client.HostAsync<ITestService>(new TestService()).Wait();

                var service = client.GetAsync<ITestService>().Result;

                Assert.NotNull(service);
            }

            [Fact]
            public void GetServiceByNameOnlyTakesNamedServices()
            {
                var connection = CreateConnection();
                var client = new ServiceDiscClient(connection);

                client.HostAsync<ITestService>(new TestService()).Wait();

                var service = client.GetAsync<ITestService>("Service1").Result;

                Assert.Null(service);
            }

            [Fact]
            public void GetServiceByNameFindsNamedServices()
            {
                var connection = CreateConnection();
                var client = new ServiceDiscClient(connection);

                client.HostAsync<ITestService>(new TestService(), "Service1").Wait();

                var service = client.GetAsync<ITestService>("Service1").Result;

                Assert.NotNull(service);
            }

            [Fact]
            public void GetServiceByNameFindsCorrectNamedService()
            {
                var connection = CreateConnection();
                var client = new ServiceDiscClient(connection);

                client.HostAsync<ITestService>(new TestService(), "Service2").Wait();

                var service = client.GetAsync<ITestService>("Service1").Result;

                Assert.Null(service);
            }

            [Fact]
            public void GetServiceByIdOnlyTakesSpecificService()
            {
                var connection = CreateConnection();
                var client = new ServiceDiscClient(connection);

                client.HostAsync<ITestService>(new TestService()).Wait();

                var service = client.GetAsync<ITestService>(Guid.NewGuid()).Result;

                Assert.Null(service);
            }

            [Fact]
            public void GetServiceByIdFindsSpecifiedService()
            {
                var connection = CreateConnection();
                var client = new ServiceDiscClient(connection);

                var hostedService = client.HostAsync<ITestService>(new TestService()).Result;

                var service = client.GetAsync<ITestService>(hostedService.Id).Result;

                Assert.NotNull(service);
            }

            [Fact]
            public void GetsProxyToService()
            {
                var connection = CreateConnection();
                var client = new ServiceDiscClient(connection);

                client.HostAsync<ITestService>(new TestService()).Wait();

                var service = client.GetAsync<ITestService>().Result;
                var sum = service.AddNumbers(2, 3);

                Assert.Equal(5, sum);
            }
        }

        public class SendAsyncMethod
        {
            [Fact]
            public void SendsMessageToQueue()
            {
                var connection = CreateConnection();
                var client = new ServiceDiscClient(connection);
                client.SendAsync(new TestMessage1 { SomeData = "123"}).Wait();
            }
        }

        public class SubscribeAsyncMethod
        {
            [Fact]
            public void ListenToIncomingMessages()
            {
                var resetEvent = new ManualResetEventSlim();
                var connection = CreateConnection();
                var client = new ServiceDiscClient(connection);

                client.SubscribeAsync<TestMessage1>(m => { if (m?.SomeData == "123") resetEvent.Set(); }).Wait();
                client.SendAsync(new TestMessage1 { SomeData = "123" }).Wait();

                resetEvent.Wait(TimeSpan.FromSeconds(5));
                Assert.True(resetEvent.IsSet);
            }

            [Fact]
            public void ReceiveMessagesByType()
            {
                var resetEvent1 = new ManualResetEventSlim();
                var resetEvent2 = new ManualResetEventSlim();
                var connection = CreateConnection();
                var client = new ServiceDiscClient(connection);

                client.SubscribeAsync<TestMessage2>(m => { if (m?.SomeData == "123") resetEvent2.Set(); }).Wait();
                client.SubscribeAsync<TestMessage1>(m => { if (m?.SomeData == "123") resetEvent1.Set(); }).Wait();
                client.SendAsync(new TestMessage1 { SomeData = "123" }).Wait();

                resetEvent1.Wait(TimeSpan.FromSeconds(5));
                resetEvent2.Wait(TimeSpan.FromSeconds(1));
                Assert.True(resetEvent1.IsSet);
                Assert.False(resetEvent2.IsSet);
            }
        }

        public interface ITestService
        {
            int AddNumbers(int num1, int num2);
        }

        public class TestService : ITestService
        {
            public int AddNumbers(int num1, int num2)
            {
                return num1 + num2;
            }
        }

        public class TestMessage1
        {
            public string SomeData { get; set; }
        }

        public class TestMessage2
        {
            public string SomeData { get; set; }
        }
    }
}
