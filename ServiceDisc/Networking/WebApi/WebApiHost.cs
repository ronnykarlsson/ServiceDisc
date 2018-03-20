using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ServiceDisc.Networking.HostnameResolution;

namespace ServiceDisc.Networking.WebApi
{
    internal class WebApiHost<T> : IHost
    {
        public string Type => "WebApiHost";
        public string Address { get; }

        readonly IWebHost _host;

        public WebApiHost(ServiceDiscNetworkResolver networkResolver, T service)
        {
            var random = new Random();

            var serviceProxy = new WebApiServiceCaller(service);
            int retryCount = 0;
            int maxRetryCount;

            int localPort;

            var assignedLocalPort = networkResolver.GetLocalPort();
            if (!assignedLocalPort.HasValue)
            {
                localPort = random.Next(5000, 6000);
                maxRetryCount = 10;
            }
            else
            {
                localPort = assignedLocalPort.Value;
                maxRetryCount = 0;
            }

            while (retryCount <= maxRetryCount)
            {
                try
                {
                    _host = new WebHostBuilder()
                        .UseKestrel()
                        .UseIISIntegration()
                        .ConfigureServices(c =>
                        {
                            c.Add(new ServiceDescriptor(typeof(IWebApiServiceCaller), serviceProxy));
                        })
                        .UseStartup<WebApiStartup>()
                        .UseUrls($"http://*:{localPort}")
                        .Build();

                    _host.Start();

                    break;
                }
                catch (IOException)
                {
                    retryCount++;
                    localPort = random.Next(5000, 6000);
                }
            }

            var publishedHostname = networkResolver.GetPublishedHostname();

            Address = $"http://{publishedHostname}:{localPort}/";
        }

        public void Close()
        {
            _host.Dispose();
        }
    }
}