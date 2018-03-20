using ServiceDisc.Networking.HostnameResolution;
using ServiceDisc.Networking.ServiceDiscConnection;

namespace ServiceDisc.Networking.QueueService
{
    public class QueueServiceHostFactory : IServiceHostFactory
    {
        public IHost CreateServiceHost<T>(T service, IServiceDiscConnection connection, ServiceDiscNetworkResolver networkResolver)
        {
            var host = new QueueServiceHost<T>(service, connection);
            return host;
        }
    }
}
