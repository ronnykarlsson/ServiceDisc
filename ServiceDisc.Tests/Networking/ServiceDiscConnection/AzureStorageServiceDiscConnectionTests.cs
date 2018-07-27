using System;
using System.IO;
using ServiceDisc.Networking.ServiceDiscConnection;
using Xunit;

namespace ServiceDisc.Tests.Networking.ServiceDiscConnection
{
    public class AzureStorageServiceDiscConnectionTests : IServiceDiscConnectionTests
    {
        public override IServiceDiscConnection CreateServiceDiscConnection()
        {
            var filename = @"..\..\..\azurestorage.txt";
            string connectionString = null;
            if (File.Exists(filename))
            {
                connectionString = File.ReadAllText(filename);
            }
            
            Skip.If(string.IsNullOrWhiteSpace(connectionString), "Azure storage connection string required in azurestorage.txt");

            return new AzureStorageServiceDiscConnection(connectionString) { MessagePollingDelay = TimeSpan.FromSeconds(1) };
        }
    }
}
