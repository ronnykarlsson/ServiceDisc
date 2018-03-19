using System.Collections.Generic;
using Newtonsoft.Json;

namespace ServiceDisc.Networking.QueueService
{
    public class QueueServiceRequestMessage
    {
        public QueueServiceRequestMessage()
        {
        }

        public QueueServiceRequestMessage(string methodName, Dictionary<string, string> parameters, string clientId, string messageId)
        {
            ClientId = clientId;
            MessageId = messageId;
            MethodName = methodName;
            Parameters = JsonConvert.SerializeObject(parameters);
        }

        public string ClientId { get; set; }
        public string MessageId { get; set; }
        public string MethodName { get; set; }
        public string Parameters { get; set; }
    }
}