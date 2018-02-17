using System;
using System.Threading.Tasks;
using ServiceDisc;
using ServiceDisc.Networking.ServiceDiscConnection;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        static async Task MainAsync(string[] args)
        {
            var serviceDisc = new ServiceDiscClient(new InMemoryServiceDiscConnection());

            for (int i = 0; i < 3; i++)
            {
                var service = new HelloService($"service{i + 1}");
                await serviceDisc.HostAsync<IHelloService>(service);
            }

            var client = await serviceDisc.GetAsync<IHelloService>();
            Console.WriteLine($"Hello: {client.Hello("John")}");
            Console.WriteLine($"Add: {client.Add(3, 5)}");

            var complexObject = new ComplexHello();
            complexObject = client.Increase(complexObject);
            Console.WriteLine($"Complex: {complexObject.Counter}");
            complexObject = client.Increase(complexObject);
            Console.WriteLine($"Complex: {complexObject.Counter}");
            complexObject = client.Increase(complexObject);
            Console.WriteLine($"Complex: {complexObject.Counter}");

            var serviceName = Guid.NewGuid().ToString();
            var namedService = new HelloService("Named service");
            await serviceDisc.HostAsync<IHelloService>(namedService, serviceName);
            var namedServiceClient = await serviceDisc.GetAsync<IHelloService>(serviceName);
            Console.WriteLine($"Hello: {namedServiceClient.Hello("John")}");

            await serviceDisc.SubscribeAsync<HelloMessage>(message => Console.WriteLine($"Hello from {message.Name}"));
            await serviceDisc.SendAsync(new HelloMessage { Name = "John" });

            Console.ReadLine();
            serviceDisc.Dispose();
        }
    }
}