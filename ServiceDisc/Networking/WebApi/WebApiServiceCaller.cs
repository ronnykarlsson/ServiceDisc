using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using ServiceDisc.Serialization;

namespace ServiceDisc.Networking.WebApi
{
    internal class WebApiServiceCaller : IWebApiServiceCaller
    {
        private readonly object _service;
        private readonly TypeSerializer _typeSerializer = new TypeSerializer();

        public WebApiServiceCaller(object service)
        {
            _service = service;
        }

        public string Call(string methodName, IQueryCollection requestQuery)
        {
            var method = _service.GetType().GetMethod(methodName);
            if (method == null) return null;

            var methodParameters = method.GetParameters();

            var parameters = new object[methodParameters.Length];

            for (var i = 0; i < methodParameters.Length; i++)
            {
                var methodParameter = methodParameters[i];
                var parameterType = methodParameter.ParameterType;

                StringValues queryValues;
                if (!requestQuery.TryGetValue(methodParameter.Name, out queryValues) || !queryValues.Any())
                {
                    return null;
                }

                var queryValue = queryValues.Single();

                object convertedValue = _typeSerializer.Deserialize(queryValue, parameterType);

                parameters[i] = convertedValue;
            }

            var result = method.Invoke(_service, parameters);

            if (result == null) return null;
            return _typeSerializer.Serialize(result);
        }
    }
}