using ServiceDisc.Networking;
using ServiceDisc.Networking.WebApi;

namespace ServiceDisc.Tests.Networking.WebApi
{
    public class WebApiClientTests : ServiceClientTests
    {
        public override IServiceHostFactory GetServiceHostFactory()
        {
            return new WebApiServiceHostFactory();
        }
    }
}
