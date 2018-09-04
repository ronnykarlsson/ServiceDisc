using ServiceDisc.Networking;
using ServiceDisc.Networking.QueueService;
using ServiceDisc.Tests.Networking.WebApi;

namespace ServiceDisc.Tests.Networking.QueueService
{
    public class QueueServiceClientTests : ServiceClientTests
    {
        public override IServiceHostFactory GetServiceHostFactory()
        {
            return new QueueServiceHostFactory();
        }
    }
}
