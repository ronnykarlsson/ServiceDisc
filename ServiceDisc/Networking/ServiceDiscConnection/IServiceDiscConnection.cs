using System;
using System.Threading.Tasks;
using ServiceDisc.Models;

namespace ServiceDisc.Networking.ServiceDiscConnection
{
    /// <summary>
    /// Connection to the ServiceDisc and methods for managing the service registry.
    /// </summary>
    public interface IServiceDiscConnection : IDisposable
    {
        Task RegisterAsync(ServiceInformation serviceInformation);
        Task UnregisterAsync(Guid id);
        Task<ServiceListDocument> GetServiceListAsync();
    }
}