using Microsoft.AspNetCore.Mvc;

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
        public ActionResult Get(string method)
        {
            var result = _webApiServiceCaller.Call(method, Request.Query);
            return Ok(result);
        }

        [HttpPost("{method}")]
        public ActionResult Post(string method)
        {
            var stream = Request.Body;
            var result = _webApiServiceCaller.Call(method, Request.Query, stream);
            return Ok(result);
        }
    }
}
