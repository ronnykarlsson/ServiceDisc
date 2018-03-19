using ServiceDisc.Networking.HostnameResolution;
using ServiceDisc.Networking.ServiceDiscConnection;

namespace ServiceDisc.Networking
{
    public interface IServiceHostFactory
    {
        IHost CreateServiceHost<T>(ServiceDiscNetworkResolver networkResolver, T service, IServiceDiscConnection connection);
    }
}