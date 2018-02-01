using ServiceDisc.Networking.HostnameResolution;
using Xunit;

namespace ServiceDisc.Tests.Networking.HostnameResolution
{
    public class ServiceDiscHostnameNetworkResolverTests
    {
        public class GetPublishedHostnameMethod
        {
            [Fact]
            public void MustNotBeEmpty()
            {
                var resolver = new ServiceDiscHostnameNetworkResolver();
                var hostname = resolver.GetPublishedHostname();
                Assert.False(string.IsNullOrEmpty(hostname));
            }

            [Fact]
            public void NotLocalHost()
            {
                var resolver = new ServiceDiscHostnameNetworkResolver();
                var hostname = resolver.GetPublishedHostname();
                Assert.NotEqual("localhost", hostname);
            }
        }
    }
}
