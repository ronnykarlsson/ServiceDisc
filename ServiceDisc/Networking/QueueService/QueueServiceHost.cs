using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using ServiceDisc.Networking.ServiceDiscConnection;
using ServiceDisc.Networking.WebApi;

namespace ServiceDisc.Networking.QueueService
{
    public class QueueServiceHost<T> : IHost
    {
        public string Type => "QueueServiceHost";
        public string Address { get; }

        public QueueServiceHost(T service, IServiceDiscConnection connection)
        {
            var queueName = GetQueueName(typeof(T));
            Address = queueName;

            var serviceProxy = new WebApiServiceCaller(service);

            connection.SubscribeAsync<QueueServiceRequestMessage>(async message =>
            {
                var responseQueueName = message.ClientId;
                string responseString;
                try
                {
                    var parameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(message.Parameters);
                    var queryCollection = new QueryCollection(parameters.ToDictionary(d => d.Key, d => new StringValues(d.Value)));
                    responseString = (string) serviceProxy.Call(message.MethodName, queryCollection, null);
                }
                catch (Exception e)
                {
                    var errorResponse = new QueueServiceResponseMessage(message.MessageId, null) { Exception = e };
                    await connection.SendMessageAsync(errorResponse, responseQueueName).ConfigureAwait(false);
                    return;
                }
                var response = new QueueServiceResponseMessage(message.MessageId, responseString);
                await connection.SendMessageAsync(response, responseQueueName).ConfigureAwait(false);
            }, queueName).Wait();
        }

        private string GetQueueName(Type serviceType)
        {
            var queueName = AzureStorageQueueHelpers.GetQueueName(serviceType);
            queueName += "-qsh";

            if (queueName.Length > 63)
            {
                queueName = queueName.Substring(queueName.Length - 63, 63);
                queueName = queueName.Trim('-');
            }

            return queueName;
        }
    }
}