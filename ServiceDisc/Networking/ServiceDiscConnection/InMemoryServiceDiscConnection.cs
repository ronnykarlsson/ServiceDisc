using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ServiceDisc.Models;

namespace ServiceDisc.Networking.ServiceDiscConnection
{
    public class InMemoryServiceDiscConnection : IServiceDiscConnection
    {
        private ConcurrentDictionary<Guid, ServiceInformation> _services = new ConcurrentDictionary<Guid, ServiceInformation>();
        private ConcurrentDictionary<string, object> _listeners = new ConcurrentDictionary<string, object>();

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

        public Task SendMessageAsync<T>(T message) where T : class
        {
            if (_listeners.TryGetValue(typeof(T).FullName, out var listeners))
            {
                foreach (var listener in (List<Action<T>>)listeners)
                {
                    listener(message);
                }
            }
            return Task.FromResult(0);
        }

        public Task SubscribeAsync<T>(Action<T> callback) where T : class
        {
            var listeners = (List<Action<T>>) _listeners.GetOrAdd(typeof(T).FullName, new List<Action<T>>());
            listeners.Add(callback);
            return Task.FromResult(0);
        }

        public void Dispose()
        {
        }
    }
}
