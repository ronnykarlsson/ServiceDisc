using ServiceDisc.Networking.HostnameResolution;
using ServiceDisc.Networking.WebApi;

namespace ServiceDisc.Networking
{
    internal class ServiceHostFactory
    {
        public IHost CreateServiceHost<T>(ServiceDiscNetworkResolver networkResolver, T service)
        {
            var host = new WebApiHost<T>(networkResolver, service);
            return host;
        }
    }
}
