using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using ServiceDisc.Models;
using ServiceDisc.Networking;
using ServiceDisc.Networking.HostnameResolution;
using ServiceDisc.Networking.ServiceDiscConnection;

namespace ServiceDisc
{
    /// <summary>
    /// Service discovery and communication
    /// </summary>
    public class ServiceDiscClient : IServiceDiscClient
    {
        private readonly List<ServiceInformation> _services = new List<ServiceInformation>();
        private readonly IServiceDiscConnection _connection;
        private readonly ServiceDiscNetworkResolver _networkResolver;
        private readonly ServiceHostFactory _serviceHostFactory;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connection">Connection to store service information.</param>
        public ServiceDiscClient(IServiceDiscConnection connection) : this(connection, ServiceDiscNetworkResolver.GetDefaultNetworkResolver())
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connection">Connection to store service information.</param>
        /// <param name="networkResolver">Used to resolve the IP of hosted services.</param>
        public ServiceDiscClient(IServiceDiscConnection connection, ServiceDiscNetworkResolver networkResolver)
        {
            _networkResolver = networkResolver;
            _connection = connection;

            _serviceHostFactory = new ServiceHostFactory();
        }

        /// <summary>
        /// Start a host for <paramref name="service"/> and register the service in the service list.
        /// </summary>
        /// <typeparam name="T">Type of service to host. This type will be used to resolve the service.</typeparam>
        /// <param name="service">An object to host as a service.</param>
        /// <param name="name">Name of service, can be used to resolve specific instances of a service.</param>
        /// <returns>Document which describes the hosted service.</returns>
        public async Task<ServiceInformation> HostAsync<T>(T service, string name = null)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));

            var host = _serviceHostFactory.CreateServiceHost(_networkResolver, service);

            var serviceInformation = new ServiceInformation(typeof(T), host);
            serviceInformation.Id = Guid.NewGuid();
            serviceInformation.Name = name == "" ? null : name;

            _services.Add(serviceInformation);

            await _connection.RegisterAsync(serviceInformation).ConfigureAwait(false);

            Trace.WriteLine($"{serviceInformation.Type} hosted on {host.Uri}");

            return serviceInformation;
        }

        /// <summary>
        /// Remove the service from service list so that it won't be possible to resolve it any longer.
        /// </summary>
        /// <param name="serviceInformation">Document describing the service to unregister.</param>
        public async Task UnregisterAsync(ServiceInformation serviceInformation)
        {
            await _connection.UnregisterAsync(serviceInformation.Id).ConfigureAwait(false);
        }

        /// <summary>
        /// Resolve a service of type <typeparamref name="T"/> and create a proxy for communicating with it.
        /// </summary>
        /// <typeparam name="T">Type of service to resolve.</typeparam>
        /// <returns>Proxy for communicating with the service.</returns>
        public async Task<T> GetAsync<T>() where T : class
        {
            var document = await _connection.GetServiceListAsync().ConfigureAwait(false);

            var serviceName = typeof(T).FullName;
            var services = document.Services.Where(s => s.Type == serviceName).ToArray();
            if (!services.Any())
            {
                return null;
            }

            var proxyType = CreateServiceProxy<T>(services);

            return proxyType;
        }

        /// <summary>
        /// Resolve the service with <paramref name="id"/> of type <typeparamref name="T"/> and create a proxy for communicating with it.
        /// </summary>
        /// <typeparam name="T">Type of service to resolve.</typeparam>
        /// <paramref name="id">Id of service to resolve.</paramref>
        /// <returns>Proxy for communicating with the service.</returns>
        public async Task<T> GetAsync<T>(Guid id) where T : class
        {
            var document = await _connection.GetServiceListAsync().ConfigureAwait(false);

            var serviceName = typeof(T).FullName;
            var services = document.Services.Where(s => s.Type == serviceName && s.Id == id).ToArray();
            if (!services.Any())
            {
                return null;
            }

            var proxyType = CreateServiceProxy<T>(services);

            return proxyType;
        }

        /// <summary>
        /// Resolve the service with <paramref name="name"/> of type <typeparamref name="T"/> and create a proxy for communicating with it.
        /// </summary>
        /// <typeparam name="T">Type of service to resolve.</typeparam>
        /// <paramref name="name">Name of service to resolve.</paramref>
        /// <returns>Proxy for communicating with the service.</returns>
        public async Task<T> GetAsync<T>(string name) where T : class
        {
            var document = await _connection.GetServiceListAsync().ConfigureAwait(false);

            var serviceName = typeof(T).FullName;
            var services = document.Services.Where(s => s.Type == serviceName && s.Name == name).ToArray();
            if (!services.Any())
            {
                return null;
            }

            var proxyType = CreateServiceProxy<T>(services);

            return proxyType;
        }

        private T CreateServiceProxy<T>(ServiceInformation[] services) where T : class
        {
            var serviceDiscCollection = new ServiceDiscCollection(services);
            serviceDiscCollection.FailedCall += ServiceDiscCollectionOnFailedCall;

            var proxyGenerator = new ProxyGenerator();
            IInterceptor serviceProxy = new ServiceProxy(serviceDiscCollection, TimeSpan.FromMinutes(5));

            var proxyType = proxyGenerator.CreateInterfaceProxyWithoutTarget<T>(serviceProxy);
            return proxyType;
        }

        private void ServiceDiscCollectionOnFailedCall(object sender, ServiceDiscCollection.FailedCallEventArgs failedCallEventArgs)
        {
            if (failedCallEventArgs.ServiceInformation.ExpireTime < DateTime.UtcNow)
            {
                _connection.UnregisterAsync(failedCallEventArgs.ServiceInformation.Id).Wait();
            }
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}
