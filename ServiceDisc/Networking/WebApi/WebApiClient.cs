using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using ServiceDisc.Models;
using ServiceDisc.Networking.ServiceClients;
using ServiceDisc.Networking.ServiceDiscConnection;
using ServiceDisc.Serialization;

namespace ServiceDisc.Networking.WebApi
{
    internal class WebApiClient : IServiceClient
    {
        private static readonly TypeSerializer _typeSerializer = new TypeSerializer();

        public async Task CallServiceAsync(IServiceDiscConnection connection, ServiceInformation service, IInvocation invocation, CancellationToken cancellationToken)
        {
            var streamParameter = invocation.Method.GetParameters().FirstOrDefault(ParameterValidation.IsStreamParameter);
            var streamArgument = invocation.Arguments.FirstOrDefault(arg => arg is Stream) as Stream;

            var serviceUrl = BuildServiceUrl(invocation, service);
            using (var client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response;
                    if (streamParameter == null || streamArgument == null)
                    {
                        // GET
                        response = await client.GetAsync(new Uri(serviceUrl), cancellationToken).ConfigureAwait(false);
                        if (response.StatusCode == HttpStatusCode.NoContent) return;
                        if (response.StatusCode != HttpStatusCode.OK) throw new ServiceDiscException($"Unexpected response from service: {response.ReasonPhrase}");
                    }
                    else
                    {
                        // POST
                        response = await client.PostAsync(new Uri(serviceUrl), new StreamContent(streamArgument), cancellationToken).ConfigureAwait(false);
                        if (response.StatusCode == HttpStatusCode.NoContent) return;
                    }

                    if (invocation.Method.ReturnType != null)
                    {
                        if (typeof(Stream).IsAssignableFrom(invocation.Method.ReturnType))
                        {
                            var streamResult = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                            invocation.ReturnValue = streamResult;
                        }
                        else
                        {
                            var stringResult = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                            var deserializedResult = _typeSerializer.Deserialize(stringResult, invocation.Method.ReturnType);
                            invocation.ReturnValue = deserializedResult;
                        }
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
            ParameterValidation.Validate(parameters);

            stringBuilder.Append(service.Address);
            stringBuilder.Append(invocation.Method.Name);

            for (var i = 0; i < parameters.Length; i++)
            {
                var invocationArgument = invocation.Arguments[i];
                if (invocationArgument == null) continue;
                if (ParameterValidation.IsStreamParameter(parameters[i])) continue;

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
