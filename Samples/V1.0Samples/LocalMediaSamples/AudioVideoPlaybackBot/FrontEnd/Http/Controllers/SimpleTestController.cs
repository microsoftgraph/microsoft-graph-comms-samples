using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Sample.AudioVideoPlaybackBot.FrontEnd.Http
{
    public class SimpleTestController : ApiController
    {
        [HttpGet]
        [Route("simpletest")]
        public HttpResponseMessage Get()
        {
            System.Diagnostics.EventLog.WriteEntry(
                "AudioVideoPlaybackService", 
                "SimpleTestController accessed", 
                System.Diagnostics.EventLogEntryType.Information);
                
            return Request.CreateResponse(HttpStatusCode.OK, "Simple test endpoint is working");
        }
    }
} 