namespace ServiceDisc.Networking.HostnameResolution
{
    public class ServiceDiscLocalHostNetworkResolver : ServiceDiscNetworkResolver
    {
        public override string GetPublishedHostname()
        {
            return "localhost";
        }
    }
}