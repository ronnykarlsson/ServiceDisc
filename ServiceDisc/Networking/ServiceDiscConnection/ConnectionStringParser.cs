using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace ServiceDisc.Networking.ServiceDiscConnection
{
    internal static class ConnectionStringParser
    {
        /// <summary>
        /// Create a <see cref="IServiceDiscConnection"/> based on <paramref name="connectionStringName"/>.
        /// </summary>
        /// <param name="connectionStringName">Name of connection string to read from environment.</param>
        /// <returns><see cref="IServiceDiscConnection"/></returns>
        /// <exception cref="ArgumentException">Value cannot be null or whitespace.</exception>
        public static IServiceDiscConnection Create(string connectionStringName)
        {
            if (string.IsNullOrWhiteSpace(connectionStringName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(connectionStringName));

            // Try to retrieve connection string from environment variables
            var connectionString = connectionStringName.Contains("=")
                ? connectionStringName
                : Environment.GetEnvironmentVariable(connectionStringName);

            if (connectionString == null)
                throw new ServiceDiscException($"Connection string not found: {connectionStringName}");

            var providerNameRegex = new Regex(@"ProviderName\s*=\s*([^;]+)\s*;?", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

            var providerNameRegeMatch = providerNameRegex.Match(connectionString);
            if (providerNameRegeMatch.Captures.Count != 1)
                throw new ServiceDiscException($"ProviderName not found in connection string: {connectionStringName}");

            var providerName = providerNameRegeMatch.Groups[1].Value;
            connectionString = providerNameRegex.Replace(connectionString, "").TrimEnd(';');

            var searchTypes = GetAllClassTypes();
            var providerType = FindProviderFromTypes(providerName, searchTypes);

            if (providerType == null) throw new ServiceDiscException($"Provider {providerName} not found.");

            var serviceDiscConnection = CreateServiceDiscConnection(providerType, connectionString);

            if (serviceDiscConnection == null)
                throw new ServiceDiscException($"Unable to create provider: {providerName}");

            return serviceDiscConnection;
        }

        private static IServiceDiscConnection CreateServiceDiscConnection(Type providerType, string connectionString)
        {
            IServiceDiscConnection serviceDiscConnection = null;

            var constructors = providerType.GetConstructors();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                // Try to create connection from empty constructor
                var emptyConstructor = constructors.FirstOrDefault(t => t.GetParameters().Length == 0);
                serviceDiscConnection = emptyConstructor?.Invoke(new object[0]) as IServiceDiscConnection;
            }

            if (serviceDiscConnection == null)
            {
                // Try to create connection from a single string constructor
                var connectionStringConstructor = constructors.FirstOrDefault(t => t.GetParameters().Length == 1 && t.GetParameters()[0].ParameterType == typeof(string));
                serviceDiscConnection = connectionStringConstructor?.Invoke(new object[] {connectionString}) as IServiceDiscConnection;
            }

            return serviceDiscConnection;
        }

        private static Type FindProviderFromTypes(string providerName, IEnumerable<Type> searchTypes)
        {
            var providerType = providerName.Contains(".")
                ? searchTypes.FirstOrDefault(t => t.FullName == providerName)
                : searchTypes.FirstOrDefault(t => t.Name == providerName);
            return providerType;
        }

        private static IEnumerable<Type> GetAllClassTypes()
        {
            var searchTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a =>
            {
                try
                {
                    return a.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    return e.Types;
                }
            }).Where(t => t.IsClass && !t.IsAbstract);
            return searchTypes;
        }
    }
}
