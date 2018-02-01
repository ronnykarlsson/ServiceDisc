using System;
using System.Diagnostics;
using System.Threading;
using Castle.DynamicProxy;
using ServiceDisc.Models;
using ServiceDisc.Networking.WebApi;

namespace ServiceDisc.Networking
{
    /// <summary>
    /// Proxy class used for connecting to and communicating with services.
    /// </summary>
    internal class ServiceProxy : IInterceptor
    {
        private readonly ServiceDiscCollection _serviceCollection;
        private readonly TimeSpan _timeoutTimeSpan;

        public ServiceProxy(ServiceDiscCollection serviceCollection, TimeSpan timeout)
        {
            _serviceCollection = serviceCollection;
            _timeoutTimeSpan = timeout;
        }

        public void Intercept(IInvocation invocation)
        {
            var cancellationTokenSource = new CancellationTokenSource(_timeoutTimeSpan);
            var cancellationToken = cancellationTokenSource.Token;

            var service = _serviceCollection.GetService();

            if (service == null)
            {
                throw new ServiceDiscException("Service not available.");
            }

            int tryCount = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Trace.WriteLine($"Trying to call service: {service}");
                var serviceClient = GetServiceClient(service);

                try
                {
                    serviceClient.CallServiceAsync(service, invocation, cancellationToken).Wait(cancellationToken);
                    break;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error while calling service: {service}\n{ex}");
                    
                    tryCount++;
                    _serviceCollection.OnFailedCall(service, ex);
                    service = _serviceCollection.GetService();

                    if (service == null)
                    {
                        throw new ServiceDiscException("Service not available.", ex);
                    }

                    if (tryCount > _serviceCollection.ServiceCount * 2)
                    {
                        throw new ServiceDiscException("Service not available, max retries exceeded.", ex);
                    }
                }
            }
        }

        private WebApiClient GetServiceClient(ServiceInformation service)
        {
            if (service.HostType == "WebApiHost")
            {
                return new WebApiClient();
            }
            throw new InvalidOperationException($"Unknown HostType: {service.HostType}");
        }
    }
}