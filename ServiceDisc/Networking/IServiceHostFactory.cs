using ServiceDisc.Networking.HostnameResolution;
using ServiceDisc.Networking.ServiceDiscConnection;

namespace ServiceDisc.Networking
{
    /// <summary>
    /// Handle creation of service hosts.
    /// </summary>
    public interface IServiceHostFactory
    {
        IHost CreateServiceHost<T>(T service, IServiceDiscConnection connection, ServiceDiscNetworkResolver networkResolver);
    }
}