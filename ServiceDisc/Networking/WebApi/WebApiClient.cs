using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using ServiceDisc.Models;
using ServiceDisc.Networking.ServiceDiscConnection;
using ServiceDisc.Serialization;

namespace ServiceDisc.Networking.WebApi
{
    internal class WebApiClient : IServiceClient
    {
        private static readonly TypeSerializer _typeSerializer = new TypeSerializer();

        public async Task CallServiceAsync(IServiceDiscConnection connection, ServiceInformation service, IInvocation invocation, CancellationToken cancellationToken)
        {
            var serviceUrl = BuildServiceUrl(invocation, service);
            using (var client = new HttpClient())
            {
                try
                {
                    var response = await client.GetAsync(new Uri(serviceUrl), cancellationToken).ConfigureAwait(false);
                    if (response.StatusCode == HttpStatusCode.NoContent) return;

                    var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    if (invocation.Method.ReturnType != null)
                    {
                        var deserializedResult = _typeSerializer.Deserialize(result, invocation.Method.ReturnType);
                        invocation.ReturnValue = deserializedResult;
                    }
                }
                catch (Exception e)
                {
                    Trace.WriteLine($"Unable to call service: {service}\n{e}");
                    throw;
                }
            }
        }

        private string BuildServiceUrl(IInvocation invocation, ServiceInformation service)
        {
            var stringBuilder = new StringBuilder();

            var parameters = invocation.Method.GetParameters();

            stringBuilder.Append(service.Address);
            stringBuilder.Append(invocation.Method.Name);

            for (var i = 0; i < parameters.Length; i++)
            {
                var invocationArgument = invocation.Arguments[i];
                if (invocationArgument == null) continue;

                stringBuilder.Append(i == 0 ? "?" : "&");

                stringBuilder.Append(parameters[i].Name);
                stringBuilder.Append("=");
                var serializedParameter = _typeSerializer.Serialize(invocationArgument);
                var encodedParameter = UrlEncoder.Default.Encode(serializedParameter);
                stringBuilder.Append(encodedParameter);
            }
            return stringBuilder.ToString();
        }
    }
}
