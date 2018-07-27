using ServiceDisc.Networking.ServiceDiscConnection;

namespace ServiceDisc.Tests.Networking.ServiceDiscConnection
{
    public class InMemoryServiceDiscConnectionTests : IServiceDiscConnectionTests
    {
        private static InMemoryServiceDiscConnection _serviceDiscConnection = new InMemoryServiceDiscConnection();
        public override IServiceDiscConnection CreateServiceDiscConnection()
        {
            return _serviceDiscConnection;
        }
    }
}
