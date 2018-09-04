using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using ServiceDisc.Models;
using ServiceDisc.Networking.ServiceClients;
using ServiceDisc.Networking.ServiceDiscConnection;
using ServiceDisc.Serialization;

namespace ServiceDisc.Networking.QueueService
{
    internal class QueueServiceClient : IServiceClient
    {
        private static readonly TypeSerializer _typeSerializer = new TypeSerializer();
        private static ConcurrentDictionary<IServiceDiscConnection, QueueServiceClientResponseQueue> _responseQueueDictionary = new ConcurrentDictionary<IServiceDiscConnection, QueueServiceClientResponseQueue>();

        public async Task CallServiceAsync(IServiceDiscConnection connection, ServiceInformation service, IInvocation invocation, CancellationToken cancellationToken)
        {
            if (ParameterValidation.IsStreamParameter(invocation.Method.ReturnParameter)
                    || invocation.Method.GetParameters().Any(ParameterValidation.IsStreamParameter))
                throw new NotSupportedException($"{nameof(Stream)} parameter isn't supported for {nameof(QueueServiceClient)} currently.");

            var queueName = service.Address;
            var parameters = BuildParameterDictionary(invocation);

            var responseQueue = _responseQueueDictionary.GetOrAdd(connection, c => new QueueServiceClientResponseQueue(c));

            var messageId = Guid.NewGuid().ToString();
            var message = new QueueServiceRequestMessage(invocation.Method.Name, parameters, responseQueue.ClientId, messageId);

            var messageReceived = false;
            string responseString = null;
            responseQueue.Subscribe(messageId, response =>
            {
                responseString = response;
                messageReceived = true;
            });

            await connection.SendMessageAsync(message, queueName).ConfigureAwait(false);

            var timeout = DateTime.UtcNow.Add(TimeSpan.FromSeconds(60));
            while (!messageReceived)
            {
                if (DateTime.UtcNow > timeout)
                {
                    throw new TimeoutException("No response from QueueServiceHost.");
                }
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }

            if (responseString != null && invocation.Method.ReturnType != null)
            {
                var deserializedResult = _typeSerializer.Deserialize(responseString, invocation.Method.ReturnType);
                invocation.ReturnValue = deserializedResult;
            }
        }

        private Dictionary<string, string> BuildParameterDictionary(IInvocation invocation)
        {
            var dictionary = new Dictionary<string, string>();

            var parameters = invocation.Method.GetParameters();

            for (var i = 0; i < parameters.Length; i++)
            {
                dictionary.Add(parameters[i].Name, _typeSerializer.Serialize(invocation.Arguments[i]));
            }
            return dictionary;
        }
    }
}
