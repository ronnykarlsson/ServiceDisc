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
        public Uri Uri { get; }

        public QueueServiceHost(T service, IServiceDiscConnection connection)
        {
            var queueName = GetQueueName(typeof(T));
            var serviceProxy = new WebApiServiceCaller(service);

            connection.SubscribeAsync<QueueServiceRequestMessage>(async message =>
            {
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(message.Parameters);
                var queryCollection = new QueryCollection(parameters.ToDictionary(d => d.Key, d => new StringValues(d.Value)));
                var responseString = serviceProxy.Call(message.MethodName, queryCollection);
                var response = new QueueServiceResponseMessage(message.MessageId, responseString);
                var responseQueueName = message.ClientId;
                await connection.SendMessageAsync(response, responseQueueName).ConfigureAwait(false);
            }, queueName).Wait();
        }

        private string GetQueueName(Type serviceType)
        {
            var queueName = serviceType.FullName.Replace(".", "-").ToLowerInvariant();

            queueName += "-qsh";

            if (queueName.Length > 63)
            {
                queueName = queueName.Substring(queueName.Length - 63, 63);
                queueName.Trim('-');
            }

            return queueName;
        }
    }
}