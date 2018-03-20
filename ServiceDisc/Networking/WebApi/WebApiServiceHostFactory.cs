using ServiceDisc.Networking.HostnameResolution;
using ServiceDisc.Networking.ServiceDiscConnection;

namespace ServiceDisc.Networking.WebApi
{
    public class WebApiServiceHostFactory : IServiceHostFactory
    {
        public IHost CreateServiceHost<T>(T service, IServiceDiscConnection connection, ServiceDiscNetworkResolver networkResolver)
        {
            var host = new WebApiHost<T>(networkResolver, service);
            return host;
        }
    }
}
