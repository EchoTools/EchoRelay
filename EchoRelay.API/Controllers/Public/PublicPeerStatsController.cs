using Microsoft.AspNetCore.Mvc;
using System.Net;
using EchoRelay.API.Public;
using EchoRelay.Core.Server;

namespace EchoRelay.API.Controllers.Public
{
    [Route("peerStats/")]
    [ApiController]
    public class PublicPeerStatsController : ControllerBase
    {
        static Server? ServerInfo => ApiServer.Instance?.RelayServer.ServerDBService.Server;

        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                if (ServerInfo == null)
                {
                    return StatusCode((int)HttpStatusCode.InternalServerError, "Registry is null");
                }

                var peerStats = new PublicPeerStats(ServerInfo);                

                return Ok(peerStats);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
