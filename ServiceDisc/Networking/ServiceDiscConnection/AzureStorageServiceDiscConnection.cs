using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using ServiceDisc.Models;
using ServiceDisc.Serialization;

namespace ServiceDisc.Networking.ServiceDiscConnection
{
    public class AzureStorageServiceDiscConnection : IServiceDiscConnection
    {
        CloudBlobClient _blobClient;
        CloudQueueClient _queueClient;
        string _containerId = "servicedisc";
        string _serviceListDocumentId = "servicelist.txt";
        static TimeSpan ExpireTimeSpan => TimeSpan.FromMinutes(2);
        public TimeSpan MessagePollingDelay { get; set; } = TimeSpan.FromSeconds(10);

        private ConcurrentDictionary<Guid, ServiceInformation> _activeServices = new ConcurrentDictionary<Guid, ServiceInformation>();
        private ConcurrentDictionary<AzureQueueMessageListenerInformation, object> _messageListeners = new ConcurrentDictionary<AzureQueueMessageListenerInformation, object>();
        private Task _expirationRefreshTask;
        private Task _messageListenerTask;
        private CancellationTokenSource _cancellationTokenSource;

        private readonly TypeSerializer _typeSerializer = new TypeSerializer();

        public AzureStorageServiceDiscConnection(string connectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            _blobClient = storageAccount.CreateCloudBlobClient();
            _queueClient = storageAccount.CreateCloudQueueClient();

            InitializeAsync().Wait();

            _cancellationTokenSource = new CancellationTokenSource();
            _expirationRefreshTask = Task.Run(async () => await HandleServiceExpirationAsync(_cancellationTokenSource.Token).ConfigureAwait(false));
            _messageListenerTask = Task.Run(async () => await HandleMessageListenersAsync(_cancellationTokenSource.Token).ConfigureAwait(false));
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
            var container = _blobClient.GetContainerReference(_containerId);
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
                var serviceList = await GetServiceListAsync().ConfigureAwait(false);
                foreach (var expiredService in serviceList.Services.Where(s => s.ExpireTime < DateTime.UtcNow))
                {
                    await UnregisterAsync(expiredService.Id).ConfigureAwait(false);
                }
            }
        }

        private CloudBlockBlob GetServiceBlob()
        {
            var container = _blobClient.GetContainerReference(_containerId);
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

        public Task SendMessageAsync<T>(T message) where T : class
        {
            return SendMessageAsync(message, GetQueueName(typeof(T)), TimeSpan.FromMinutes(1));
        }

        public Task SendMessageAsync<T>(T message, string name) where T : class
        {
            return SendMessageAsync(message, name, TimeSpan.FromMinutes(1));
        }

        public async Task SendMessageAsync<T>(T message, string name, TimeSpan timeToLive)
        {
            var queueReference = _queueClient.GetQueueReference(name);
            await queueReference.CreateIfNotExistsAsync().ConfigureAwait(false);
            var textMessage = _typeSerializer.Serialize(message);
            var queueMessage = new CloudQueueMessage(textMessage);
            await queueReference.AddMessageAsync(queueMessage, timeToLive, null, new QueueRequestOptions(), new OperationContext()).ConfigureAwait(false);
        }

        public Task SubscribeAsync<T>(Action<T> callback) where T : class
        {
            return SubscribeAsync(callback, GetQueueName(typeof(T)));
        }

        public async Task SubscribeAsync<T>(Action<T> callback, string name) where T : class
        {
            var queueReference = _queueClient.GetQueueReference(name);
            await queueReference.CreateIfNotExistsAsync().ConfigureAwait(false);
            _messageListeners.TryAdd(new AzureQueueMessageListenerInformation(name, typeof(T)), callback);
        }

        private async Task HandleMessageListenersAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested) return;

                var processedMessages = 0;

                foreach (var messageListenerInformation in _messageListeners.Keys.ToArray())
                {
                    if (_messageListeners.TryGetValue(messageListenerInformation, out object callback))
                    {
                        var queueReference = _queueClient.GetQueueReference(messageListenerInformation.QueueName);
                        await queueReference.CreateIfNotExistsAsync().ConfigureAwait(false);
                        var message = await queueReference.GetMessageAsync().ConfigureAwait(false);
                        if (message != null)
                        {
                            var deserializedMessage = _typeSerializer.Deserialize(message.AsString, messageListenerInformation.MessageType);
                            var methodInfo = callback.GetType().GetMethod("Invoke");
                            methodInfo.Invoke(callback, new[] {deserializedMessage});
                            await queueReference.DeleteMessageAsync(message);
                            processedMessages++;
                        }
                    }

                    if (cancellationToken.IsCancellationRequested) return;
                }

                if (processedMessages == 0)
                {
                    await Task.Delay(MessagePollingDelay, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private string GetQueueName(Type messageType)
        {
            var queueName = messageType.FullName.Replace(".", "-").ToLowerInvariant();

            if (queueName.Length < 3)
            {
                queueName += "-q";
            }
            else if (queueName.Length > 63)
            {
                queueName = queueName.Substring(queueName.Length - 63, 63);
                queueName.Trim('-');
            }

            return queueName;
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();

            try
            {
                _expirationRefreshTask?.Dispose();
            }
            catch { }

            try
            {
                _messageListenerTask?.Dispose();
            }
            catch { }
        }
    }
}