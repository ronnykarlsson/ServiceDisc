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
        Task SendMessageAsync<T>(T message) where T : class;
        Task SubscribeAsync<T>(Action<T> callback) where T : class;
    }
}