namespace ServiceDisc.Networking.QueueService
{
    public class QueueServiceResponseMessage
    {
        public QueueServiceResponseMessage()
        {
        }

        public QueueServiceResponseMessage(string messageId, string response)
        {
            MessageId = messageId;
            Response = response;
        }

        public string MessageId { get; set; }
        public string Response { get; set; }
    }
}