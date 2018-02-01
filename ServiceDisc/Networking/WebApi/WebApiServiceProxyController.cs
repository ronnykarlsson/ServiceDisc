using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ServiceDisc.Networking.WebApi
{
    /// <summary>
    /// Controller for taking calls to pass on to the server instance.
    /// </summary>
    [Route("")]
    public class WebApiServiceProxyController : Controller
    {
        private readonly IWebApiServiceCaller _webApiServiceCaller;

        public WebApiServiceProxyController(IWebApiServiceCaller webApiServiceCaller)
        {
            _webApiServiceCaller = webApiServiceCaller;
        }

        [HttpGet("{method}")]
        public string Get(string method)
        {
            var result = _webApiServiceCaller.Call(method, Request.Query);
            return result;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
        }
    }
}
