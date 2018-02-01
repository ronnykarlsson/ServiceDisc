using System;
using System.Collections;

namespace ServiceDisc.Networking.HostnameResolution
{
    /// <summary>
    /// Resolve IP to register service as
    /// </summary>
    public abstract class ServiceDiscNetworkResolver
    {
        public static ServiceDiscNetworkResolver Localhost = new ServiceDiscLocalHostNetworkResolver();
        public static ServiceDiscNetworkResolver External = new ServiceDiscExternalNetworkResolver();
        public static ServiceDiscNetworkResolver Hostname = new ServiceDiscHostnameNetworkResolver();

        const string ServicediscPortEnvironmentVariable = "SERVICEDISC_PORT";
        const string ServicediscLocalPortEnvironmentVariable = "SERVICEDISC_LOCALPORT";
        const string ServicediscExternalPortEnvironmentVariable = "SERVICEDISC_EXTERNALPORT";
        const string ServicediscNetworkEnvironmentVariable = "SERVICEDISC_NETWORK";

        public abstract string GetPublishedHostname();

        public virtual int? GetLocalPort()
        {
            var environmentVariables = Environment.GetEnvironmentVariables();

            var portString = GetEnvironmentVariable(environmentVariables, ServicediscLocalPortEnvironmentVariable)
                             ?? GetEnvironmentVariable(environmentVariables, ServicediscPortEnvironmentVariable);

            if (portString == null) return null;

            int port;
            if (!int.TryParse(portString, out port)) return null;

            return port;
        }

        public virtual int GetExternalPort(int localPort)
        {
            var environmentVariables = Environment.GetEnvironmentVariables();

            var portString = GetEnvironmentVariable(environmentVariables, ServicediscExternalPortEnvironmentVariable)
                             ?? GetEnvironmentVariable(environmentVariables, ServicediscPortEnvironmentVariable);

            if (portString == null) return localPort;

            int port;
            if (!int.TryParse(portString, out port)) return localPort;

            return port;
        }

        public static ServiceDiscNetworkResolver GetDefaultNetworkResolver()
        {
            var environmentVariables = Environment.GetEnvironmentVariables();
            var networkResolver = GetEnvironmentVariable(environmentVariables, ServicediscNetworkEnvironmentVariable);
            if (string.IsNullOrEmpty(networkResolver))
            {
                return Hostname;
            }

            switch (networkResolver.ToLowerInvariant())
            {
                case "localhost":
                    return Localhost;
                case "external":
                    return External;
                case "hostname":
                    return Hostname;
                default:
                    throw new InvalidOperationException($"Invalid {ServicediscNetworkEnvironmentVariable} specified: {networkResolver}");
            }
        }

        protected static string GetEnvironmentVariable(IDictionary environmentVariables, string variableName)
        {
            if (!environmentVariables.Contains(variableName)) return null;
            var result = environmentVariables[variableName] as string;
            return result;
        }
    }
}
