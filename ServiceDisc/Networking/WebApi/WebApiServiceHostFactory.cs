using ServiceDisc.Networking.HostnameResolution;
using ServiceDisc.Networking.ServiceDiscConnection;

namespace ServiceDisc.Networking.WebApi
{
    public class WebApiServiceHostFactory : IServiceHostFactory
    {
        public IHost CreateServiceHost<T>(ServiceDiscNetworkResolver networkResolver, T service, IServiceDiscConnection connection)
        {
            var host = new WebApiHost<T>(networkResolver, service);
            return host;
        }
    }
}
