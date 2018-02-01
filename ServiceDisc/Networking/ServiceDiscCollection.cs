using System;
using System.Linq;
using ServiceDisc.Models;

namespace ServiceDisc.Networking
{
    /// <summary>
    /// Information about registered services
    /// </summary>
    internal class ServiceDiscCollection
    {
        private static readonly Random _random = new Random();

        private readonly object _syncObject = new object();

        private readonly ServiceInformation[] _allServices;
        private ServiceInformation[] _services;

        public int ServiceCount { get; }

        public event EventHandler<FailedCallEventArgs> FailedCall;

        public ServiceDiscCollection(ServiceInformation[] services)
        {
            _allServices = services;
            _services = services;
            ServiceCount = _allServices.Length;
        }

        public ServiceInformation GetService()
        {
            var currentServices = _services;

            if (currentServices.Length == 0)
                lock (_syncObject)
                    if (_services.Length == 0)
                        currentServices = _services = _allServices;

            if (currentServices.Length == 0)
            {
                return null;
            }

            if (currentServices.Length == 1)
            {
                return currentServices[0];
            }

            var service = currentServices[_random.Next(currentServices.Length)];
            return service;
        }

        public void OnFailedCall(ServiceInformation serviceInformation, Exception httpRequestException)
        {
            lock (_syncObject)
            {
                var newServices = _services.Where(s => s != serviceInformation).ToArray();
                _services = newServices;
            }

            FailedCall?.Invoke(this, new FailedCallEventArgs(serviceInformation));
        }

        public class FailedCallEventArgs : EventArgs
        {
            public FailedCallEventArgs(ServiceInformation serviceInformation)
            {
                ServiceInformation = serviceInformation;
            }

            public ServiceInformation ServiceInformation { get; set; }
        }
    }
}