using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using ServiceDisc.Models;
using ServiceDisc.Networking.ServiceDiscConnection;

namespace ServiceDisc.Networking
{
    internal interface IServiceClient
    {
        Task CallServiceAsync(IServiceDiscConnection connection, ServiceInformation service, IInvocation invocation, CancellationToken cancellationToken);
    }
}