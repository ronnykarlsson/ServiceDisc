using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using ServiceDisc.Models;

namespace ServiceDisc.Networking.ServiceDiscConnection
{
    public class AzureBlobServiceDiscConnection : IServiceDiscConnection
    {
        CloudBlobClient _client;
        string _containerId = "servicedisc";
        string _serviceListDocumentId = "servicelist.txt";
        static TimeSpan ExpireTimeSpan => TimeSpan.FromMinutes(2);

        private ConcurrentDictionary<Guid, ServiceInformation> _activeServices = new ConcurrentDictionary<Guid, ServiceInformation>();
        private Task _expirationRefreshTask;
        private CancellationTokenSource _refreshCancellationTokenSource;

        public AzureBlobServiceDiscConnection(string connectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            _client = storageAccount.CreateCloudBlobClient();

            InitializeAsync().Wait();

            _refreshCancellationTokenSource = new CancellationTokenSource();
            _expirationRefreshTask = Task.Run(async () => await HandleServiceExpirationAsync(_refreshCancellationTokenSource.Token).ConfigureAwait(false));
        }

        private async Task HandleServiceExpirationAsync(CancellationToken cancellationToken)
        {
            var refreshTimeSpan = TimeSpan.FromMilliseconds(ExpireTimeSpan.TotalMilliseconds / 2);

            while (true)
            {
                if (cancellationToken.IsCancellationRequested) break;

                await RefreshExpirationAsync(_activeServices.Select(s => s.Value).ToArray()).ConfigureAwait(false);

                await Task.Delay(refreshTimeSpan, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task InitializeAsync()
        {
            var container = _client.GetContainerReference(_containerId);
            await container.CreateIfNotExistsAsync().ConfigureAwait(false);

            var blob = GetServiceBlob();
            var exists = await blob.ExistsAsync().ConfigureAwait(false);
            if (!exists)
            {
                // Create new service list
                var emptyServiceListDocument = new ServiceListDocument();
                var jsonDocument = JsonConvert.SerializeObject(emptyServiceListDocument);
                try
                {
                    await blob.UploadTextAsync(jsonDocument, AccessCondition.GenerateIfNotExistsCondition(), new BlobRequestOptions(), new OperationContext()).ConfigureAwait(false);
                }
                catch (StorageException e)
                {
                    if (e.Message != null && e.Message.Contains("blob already exists"))
                    {
                        throw;
                    }
                }
            }
            else
            {
                // Unregister expired services
                var serviceList = GetServiceListAsync().Result;
                foreach (var expiredService in serviceList.Services.Where(s => s.ExpireTime < DateTime.UtcNow))
                {
                    UnregisterAsync(expiredService.Id).Wait();
                }
            }
        }

        private CloudBlockBlob GetServiceBlob()
        {
            var container = _client.GetContainerReference(_containerId);
            var blob = container.GetBlockBlobReference(_serviceListDocumentId);
            return blob;
        }

        public async Task RegisterAsync(ServiceInformation serviceInformation)
        {
            var blob = GetServiceBlob();

            for (int retryCount = 0; retryCount < 100; retryCount++)
            {
                try
                {
                    var response = await blob.DownloadTextAsync().ConfigureAwait(false);
                    var serviceList = JsonConvert.DeserializeObject<ServiceListDocument>(response);

                    if (serviceList.Services == null)
                    {
                        serviceList.Services = new List<ServiceInformation>();
                    }

                    serviceInformation.ExpireTime = DateTime.UtcNow.Add(ExpireTimeSpan);
                    serviceList.Services.Add(serviceInformation);

                    var newDocument = JsonConvert.SerializeObject(serviceList);
                    var accessCondition = AccessCondition.GenerateIfMatchCondition(blob.Properties.ETag);

                    await blob.UploadTextAsync(newDocument, accessCondition, new BlobRequestOptions(), new OperationContext()).ConfigureAwait(false);
                    while (!_activeServices.TryAdd(serviceInformation.Id, serviceInformation))
                    {
                        _activeServices.TryRemove(serviceInformation.Id, out var removedServiceInformation);
                    }

                    break;
                }
                catch (StorageException e)
                {
                    if (e.Message.Contains("condition specified using HTTP conditional header(s) is not met")) continue;

                    Trace.WriteLine($"Error while registering monitor.\n{e}");
                    throw;
                }
            }
        }

        public async Task RefreshExpirationAsync(IEnumerable<ServiceInformation> serviceInformationList)
        {
            var blob = GetServiceBlob();

            for (int retryCount = 0; retryCount < 100; retryCount++)
            {
                try
                {
                    var response = await blob.DownloadTextAsync().ConfigureAwait(false);
                    var serviceList = JsonConvert.DeserializeObject<ServiceListDocument>(response);

                    if (serviceList.Services == null)
                    {
                        serviceList.Services = new List<ServiceInformation>();
                    }

                    foreach (var serviceInformation in serviceInformationList)
                    {
                        var existingService = serviceList.Services.FirstOrDefault(s => s.Id == serviceInformation.Id);
                        if (existingService == null)
                        {
                            existingService = serviceInformation;
                            serviceList.Services.Add(existingService);
                        }

                        existingService.ExpireTime = DateTime.UtcNow.Add(ExpireTimeSpan);
                    }

                    var newDocument = JsonConvert.SerializeObject(serviceList);
                    var accessCondition = AccessCondition.GenerateIfMatchCondition(blob.Properties.ETag);

                    await blob.UploadTextAsync(newDocument, accessCondition, new BlobRequestOptions(), new OperationContext()).ConfigureAwait(false);

                    break;
                }
                catch (StorageException e)
                {
                    if (e.Message.Contains("condition specified using HTTP conditional header(s) is not met")) continue;

                    Trace.WriteLine($"Error while registering monitor.\n{e}");
                    throw;
                }
            }
        }

        public async Task UnregisterAsync(Guid id)
        {
            var blob = GetServiceBlob();

            for (int retryCount = 0; retryCount < 100; retryCount++)
            {
                try
                {
                    var response = await blob.DownloadTextAsync().ConfigureAwait(false);
                    var serviceList = JsonConvert.DeserializeObject<ServiceListDocument>(response);
                    if (serviceList.Services == null) return;

                    var count = serviceList.Services.RemoveAll(doc => doc.Id == id);
                    if (count > 0)
                    {
                        var newDocument = JsonConvert.SerializeObject(serviceList);
                        var accessCondition = AccessCondition.GenerateIfMatchCondition(blob.Properties.ETag);
                        await blob.UploadTextAsync(newDocument, accessCondition, new BlobRequestOptions(), new OperationContext()).ConfigureAwait(false);
                    }

                    break;
                }
                catch (StorageException e)
                {
                    if (e.Message.Contains("condition specified using HTTP conditional header(s) is not met")) continue;

                    Trace.WriteLine($"Error while unregistering monitor.\n{e}");
                    throw;
                }
            }
        }

        public async Task<ServiceListDocument> GetServiceListAsync()
        {
            var blob = GetServiceBlob();

            var response = await blob.DownloadTextAsync().ConfigureAwait(false);
            var serviceList = JsonConvert.DeserializeObject<ServiceListDocument>(response);

            return serviceList;
        }

        public void Dispose()
        {
            _refreshCancellationTokenSource.Cancel();
            try
            {
                _expirationRefreshTask?.Dispose();
            }
            catch (Exception)
            {
            }
        }
    }
}