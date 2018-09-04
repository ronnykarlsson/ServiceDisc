using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace ServiceDisc.Networking.ServiceClients
{
    internal static class ParameterValidation
    {
        public static void Validate(IEnumerable<ParameterInfo> parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            var streamCount = 0;
            foreach (var parameter in parameters)
            {
                if (IsStreamParameter(parameter)) streamCount++;
            }

            if (streamCount > 1) throw new ServiceDiscException($"Only one {nameof(Stream)} parameter is allowed. {streamCount} {nameof(Stream)} parameters found.");
        }

        public static bool IsStreamParameter(ParameterInfo parameter)
        {
            return typeof(Stream).IsAssignableFrom(parameter.ParameterType);
        }
    }
}
