using System;

namespace ServiceDisc
{
    [Serializable]
    public class ServiceDiscException : Exception
    {
        public ServiceDiscException()
        {
        }

        public ServiceDiscException(string message) : base(message)
        {
        }

        public ServiceDiscException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}