using ServiceDisc.Models;
using ServiceDisc.Networking.ServiceDiscConnection;
using System;
using System.Collections.Concurrent;
using System.Threading;
using Xunit;

namespace ServiceDisc.Tests.Networking.ServiceDiscConnection
{
    [Trait("Category", "Integration")]
    public abstract class IServiceDiscConnectionTests
    {
        public abstract IServiceDiscConnection CreateServiceDiscConnection();

        [SkippableFact]
        public void SendAndReceiveSimpleMessage()
        {
            var connection1 = CreateServiceDiscConnection();
            var connection2 = CreateServiceDiscConnection();

            var receivedMessages = new ConcurrentBag<SimpleTestMessage>();

            connection1.SubscribeAsync<SimpleTestMessage>(message =>
            {
                receivedMessages.Add(message);
            }).GetAwaiter().GetResult();

            connection2.SendMessageAsync(SimpleTestMessage.CreateTestMessage(4)).GetAwaiter().GetResult();
            connection2.SendMessageAsync(SimpleTestMessage.CreateTestMessage(5)).GetAwaiter().GetResult();
            connection2.SendMessageAsync(SimpleTestMessage.CreateTestMessage(6)).GetAwaiter().GetResult();

            var timeout = DateTime.UtcNow.AddSeconds(10);
            while (receivedMessages.Count < 3 && DateTime.UtcNow < timeout)
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            Assert.Equal(3, receivedMessages.Count);
            Assert.Contains(receivedMessages, m => m.Id == 4 && m.IsTestMessage());
            Assert.Contains(receivedMessages, m => m.Id == 5 && m.IsTestMessage());
            Assert.Contains(receivedMessages, m => m.Id == 6 && m.IsTestMessage());
        }

        [SkippableFact]
        public void TestServiceRegistrationAndUnregistration()
        {
            var connection = CreateServiceDiscConnection();

            var serviceId = Guid.NewGuid();
            var serviceInformation = new ServiceInformation(typeof(int), new IntegrationTestHost())
            {
                Id = serviceId
            };

            // Register
            connection.RegisterAsync(serviceInformation).GetAwaiter().GetResult();
            var serviceList = connection.GetServiceListAsync().GetAwaiter().GetResult();
            Assert.Contains(serviceList.Services, si => si.Id == serviceId);

            // Unregister
            connection.UnregisterAsync(serviceId).GetAwaiter().GetResult();
            serviceList = connection.GetServiceListAsync().GetAwaiter().GetResult();
            Assert.DoesNotContain(serviceList.Services, si => si.Id == serviceId);
        }

        public class IntegrationTestHost : IHost
        {
            public string Type => "IntegrationTest";

            public string Address => "http://integrationtest";
        }

        public class SimpleTestMessage
        {
            public int Id { get; set; }
            public int TestInteger1 { get; set; }
            public int TestInteger2 { get; set; }
            public uint UnsignedInteger { get; set; }
            public long TestLong1 { get; set; }
            public long TestLong2 { get; set; }
            public ulong UnsignedLong { get; set; }
            public float TestFloat { get; set; }
            public double TestDouble { get; set; }
            public object TestObject { get; set; }
            public string TestString { get; set; }
            public bool TestBool { get; set; }

            public static SimpleTestMessage CreateTestMessage(int id)
            {
                return new SimpleTestMessage
                {
                    Id = id,
                    TestInteger1 = int.MaxValue,
                    TestInteger2 = int.MinValue,
                    UnsignedInteger = uint.MaxValue,
                    TestLong1 = long.MaxValue,
                    TestLong2 = long.MinValue,
                    UnsignedLong = ulong.MaxValue,
                    TestFloat = -13.5f,
                    TestDouble = double.Epsilon,
                    TestString = "\0Abc123€",
                    TestBool = true
                };
            }

            public bool IsTestMessage()
            {
                return
                    TestInteger1 == int.MaxValue
                    && TestInteger2 == int.MinValue
                    && UnsignedInteger == uint.MaxValue
                    && TestLong1 == long.MaxValue
                    && TestLong2 == long.MinValue
                    && UnsignedLong == ulong.MaxValue
                    && TestFloat == -13.5f
                    && TestDouble == double.Epsilon
                    && TestObject == null
                    && TestString == "\0Abc123€"
                    && TestBool == true;
            }
        }
    }
}
