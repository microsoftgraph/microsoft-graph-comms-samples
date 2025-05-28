using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Sample.AudioVideoPlaybackBot.FrontEnd.Http
{
    public class TestController : ApiController
    {
        [HttpGet]
        [Route("api/test")]
        public HttpResponseMessage Get()
        {
            return Request.CreateResponse(HttpStatusCode.OK, "Test endpoint is working");
        }
    }
} 