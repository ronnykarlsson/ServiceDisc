using System;

namespace ServiceDisc.Networking.ServiceDiscConnection
{
    class AzureQueueMessageListenerInformation : IEquatable<AzureQueueMessageListenerInformation>
    {
        public AzureQueueMessageListenerInformation()
        {
        }

        public AzureQueueMessageListenerInformation(string queueName, Type messageType)
        {
            QueueName = queueName;
            MessageType = messageType;
        }

        public bool Equals(AzureQueueMessageListenerInformation other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(QueueName, other.QueueName) && Equals(MessageType, other.MessageType);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AzureQueueMessageListenerInformation) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((QueueName != null ? QueueName.GetHashCode() : 0) * 397) ^ (MessageType != null ? MessageType.GetHashCode() : 0);
            }
        }

        public string QueueName { get; }
        public Type MessageType { get; }
    }
}