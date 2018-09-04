using System.IO;
using Microsoft.AspNetCore.Http;

namespace ServiceDisc.Networking.WebApi
{
    public interface IWebApiServiceCaller
    {
        object Call(string methodName, IQueryCollection requestQuery, Stream stream = null);
    }
}