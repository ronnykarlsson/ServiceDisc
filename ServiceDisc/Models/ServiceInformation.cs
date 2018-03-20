using System;

namespace ServiceDisc.Models
{
    /// <summary>
    /// Description of a hosted service which can be used for communicating with or managing the service.
    /// </summary>
    public class ServiceInformation
    {
        public ServiceInformation()
        {
        }

        public ServiceInformation(Type type, IHost host)
        {
            Type = type.FullName;
            Address = host.Address;
            HostType = host.Type;
        }

        /// <summary>
        /// Unique Id of the service.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Type of service hosted, the interface used to resolve it and communicate through.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Name of service. Name isn't required but it can be used to resolve services by name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Type of service host.
        /// </summary>
        public string HostType { get; set; }

        /// <summary>
        /// The endpoint on which to reach the service.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// The UTC DateTime on which the service expires.
        /// </summary>
        public DateTime ExpireTime { get; set; }

        public override string ToString()
        {
            var serviceNameString = string.IsNullOrEmpty(Name) ? Type : $"{Type} ({Name})";
            return $"{Id}: {serviceNameString} - {Address}";
        }
    }
}