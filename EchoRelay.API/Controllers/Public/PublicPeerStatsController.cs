using Microsoft.AspNetCore.Mvc;
using System.Net;
using EchoRelay.API.Public;
using EchoRelay.Core.Server;

namespace EchoRelay.API.Controllers.Public
{
    [Route("centralapi/peerstats/")]
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
                    return StatusCode((int)HttpStatusCode.NotFound, "No server found");
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
