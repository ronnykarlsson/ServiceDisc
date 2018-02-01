using ServiceDisc.Networking.HostnameResolution;
using Xunit;

namespace ServiceDisc.Tests.Networking.HostnameResolution
{
    public class ServiceDiscLocalHostNetworkResolverTests
    {
        public class GetPublishedHostnameMethod
        {
            [Fact]
            public void AlwaysLocalHost()
            {
                var resolver = new ServiceDiscLocalHostNetworkResolver();
                var hostname = resolver.GetPublishedHostname();
                Assert.Equal("localhost", hostname);
            }
        }
    }
}
