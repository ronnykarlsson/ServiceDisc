using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace ServiceDisc.Networking.HostnameResolution
{
    public class ServiceDiscHostnameNetworkResolver : ServiceDiscNetworkResolver
    {
        public override string GetPublishedHostname()
        {

            var ethernetInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(i => i.NetworkInterfaceType == NetworkInterfaceType.Ethernet &&
                            i.OperationalStatus == OperationalStatus.Up);

            var ipAddresses = ethernetInterfaces.Select(i => i.GetIPProperties())
                .Where(i => i.GatewayAddresses.Any())
                .SelectMany(i => i.UnicastAddresses.Where(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork))
                .ToArray();

            if (ipAddresses.Any())
            {
                return ipAddresses.First().Address.ToString();
            }

            var addresses = Dns.GetHostAddressesAsync(Dns.GetHostName()).GetAwaiter().GetResult();
            var address = addresses.FirstOrDefault(m => m.AddressFamily == AddressFamily.InterNetwork);
            if (address != null) return address.ToString();

            return "localhost";
        }
    }
}