using Microsoft.AspNetCore.Http;

namespace ServiceDisc.Networking.WebApi
{
    public interface IWebApiServiceCaller
    {
        string Call(string methodName, IQueryCollection requestQuery);
    }
}