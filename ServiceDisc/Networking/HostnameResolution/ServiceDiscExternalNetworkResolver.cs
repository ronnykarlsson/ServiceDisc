using System.Net.Http;
using System.Text.RegularExpressions;

namespace ServiceDisc.Networking.HostnameResolution
{
    public class ServiceDiscExternalNetworkResolver : ServiceDiscNetworkResolver
    {
        protected static readonly string ExternalIp = GetExternalIp();

        public static string GetExternalIp()
        {
            try
            {
                var externalIp = new HttpClient().GetStringAsync("http://checkip.dyndns.org/").GetAwaiter().GetResult();
                externalIp = new Regex(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}").Matches(externalIp)[0].ToString();
                return externalIp;
            }
            catch { return null; }
        }

        public override string GetPublishedHostname()
        {
            return ExternalIp;
        }
    }
}