using System;
using System.Collections.Concurrent;
using ServiceDisc.Networking.ServiceDiscConnection;

namespace ServiceDisc.Networking.QueueService
{
    internal class QueueServiceClientResponseQueue
    {
        private readonly ConcurrentDictionary<string, Action<QueueServiceResponseMessage>> _callbackDictionary = new ConcurrentDictionary<string, Action<QueueServiceResponseMessage>>();

        public string ClientId { get; }

        public QueueServiceClientResponseQueue(IServiceDiscConnection connection)
        {
            ClientId = Guid.NewGuid().ToString();

            connection.SubscribeAsync<QueueServiceResponseMessage>(ResponseCallback, ClientId).GetAwaiter().GetResult();
        }

        public void Subscribe(string messageId, Action<QueueServiceResponseMessage> callback)
        {
            // TODO remove callbacks after timeout

            if (!_callbackDictionary.TryAdd(messageId, message =>
            {
                callback(message);
                _callbackDictionary.TryRemove(messageId, out _);
            }))
            {
                throw new InvalidOperationException($"{messageId} is already subscribed.");
            }
        }

        private void ResponseCallback(QueueServiceResponseMessage message)
        {
            if (_callbackDictionary.TryGetValue(message.MessageId, out var callback))
            {
                callback(message);
            }
        }
    }
}