using Microsoft.AspNetCore.Mvc;
using System.Net;
using EchoRelay.API.Public;
using EchoRelay.Core.Server;
using Serilog;

namespace EchoRelay.API.Controllers.Public
{
    [Route("centralapi/relay/")]
    [ApiController]
    public class PublicServersController : ControllerBase
    {
        static Server? ServiceServer => ApiServer.Instance?.RelayServer.ServerDBService.Server;

        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                if (ServiceServer == null)
                {
                    return StatusCode((int)HttpStatusCode.InternalServerError, "Server is null");
                }

                var serverInfo = new PublicServerInfo(ServiceServer);
                Log.Debug("Returning server info");
                return Ok(serverInfo);
            }
            catch (Exception ex)
            {
                Log.Error("Error getting public sessions: {0}", ex.Message);
                return BadRequest(ex.Message);
            }
        }
    }
}
