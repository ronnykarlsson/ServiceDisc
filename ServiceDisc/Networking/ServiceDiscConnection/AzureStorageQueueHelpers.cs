using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ServiceDisc.Networking.ServiceDiscConnection
{
    internal class AzureStorageQueueHelpers
    {
        public static string GetQueueName(Type messageType)
        {
            if (messageType == null) throw new ArgumentNullException(nameof(messageType));
            if (messageType.FullName == null) throw new ArgumentException($"Invalid type, expecting {messageType.FullName}", nameof(messageType));

            var queueName = Regex.Replace(messageType.FullName.ToLowerInvariant(), @"[^a-z0-9-]+", "-");

            if (queueName.Length < 3)
            {
                queueName += "-q";
            }
            else if (queueName.Length > 63)
            {
                queueName = queueName.Substring(queueName.Length - 63, 63);
                queueName = queueName.Trim('-');
            }

            return queueName;
        }
    }
}
