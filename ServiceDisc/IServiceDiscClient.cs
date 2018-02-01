using System;
using System.Threading.Tasks;
using ServiceDisc.Models;

namespace ServiceDisc
{
    public interface IServiceDiscClient : IDisposable
    {
        /// <summary>
        /// Start a host for <paramref name="service"/> and register the service in the service list.
        /// </summary>
        /// <typeparam name="T">Type of service to host. This type will be used to resolve the service.</typeparam>
        /// <param name="service">An object to host as a service.</param>
        /// <param name="name">Name of service, can be used to resolve specific instances of a service.</param>
        /// <returns>Document which describes the hosted service.</returns>
        Task<ServiceInformation> HostAsync<T>(T service, string name = null);

        /// <summary>
        /// Remove the service from service list so that it won't be possible to resolve it any longer.
        /// </summary>
        /// <param name="serviceInformation">Document describing the service to unregister.</param>
        Task UnregisterAsync(ServiceInformation serviceInformation);

        /// <summary>
        /// Resolve a service of type <typeparamref name="T"/> and create a proxy for communicating with it.
        /// </summary>
        /// <typeparam name="T">Type of service to resolve.</typeparam>
        /// <returns>Proxy for communicating with the service.</returns>
        Task<T> GetAsync<T>() where T : class;

        /// <summary>
        /// Resolve the service with <paramref name="id"/> of type <typeparamref name="T"/> and create a proxy for communicating with it.
        /// </summary>
        /// <typeparam name="T">Type of service to resolve.</typeparam>
        /// <paramref name="id">Id of service to resolve.</paramref>
        /// <returns>Proxy for communicating with the service.</returns>
        Task<T> GetAsync<T>(Guid id) where T : class;

        /// <summary>
        /// Resolve the service with <paramref name="name"/> of type <typeparamref name="T"/> and create a proxy for communicating with it.
        /// </summary>
        /// <typeparam name="T">Type of service to resolve.</typeparam>
        /// <paramref name="name">Name of service to resolve.</paramref>
        /// <returns>Proxy for communicating with the service.</returns>
        Task<T> GetAsync<T>(string name) where T : class;
    }
}