using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using ServiceDisc.Models;

namespace ServiceDisc.Networking.ServiceDiscConnection
{
    public class InMemoryServiceDiscConnection : IServiceDiscConnection
    {
        private ConcurrentDictionary<Guid, ServiceInformation> _services = new ConcurrentDictionary<Guid, ServiceInformation>();

        public Task RegisterAsync(ServiceInformation serviceInformation)
        {
            _services.TryAdd(serviceInformation.Id, serviceInformation);
            return Task.FromResult(0);
        }

        public Task UnregisterAsync(Guid serviceId)
        {
            _services.TryRemove(serviceId, out var serviceInformation);
            return Task.FromResult(0);
        }

        public Task<ServiceListDocument> GetServiceListAsync()
        {
            var serviceList = new ServiceListDocument
            {
                Services = _services.Select(s => s.Value).ToList()
            };

            return Task.FromResult(serviceList);
        }

        public void Dispose()
        {
        }
    }
}
