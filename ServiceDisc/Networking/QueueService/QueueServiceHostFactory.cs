using ServiceDisc.Networking.HostnameResolution;
using ServiceDisc.Networking.ServiceDiscConnection;

namespace ServiceDisc.Networking.QueueService
{
    public class QueueServiceHostFactory : IServiceHostFactory
    {
        public IHost CreateServiceHost<T>(ServiceDiscNetworkResolver networkResolver, T service, IServiceDiscConnection connection)
        {
            var host = new QueueServiceHost<T>(service, connection);
            return host;
        }
    }
}
