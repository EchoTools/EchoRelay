using Microsoft.AspNetCore.Mvc;
using System.Net;
using EchoRelay.API.Public;
using EchoRelay.Core.Server;
using Serilog;

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
                Log.Debug("Returning peer stats");
                return Ok(peerStats);
            }
            catch (Exception ex)
            {
                Log.Error("Error getting public sessions: {0}", ex.Message);
                return BadRequest(ex.Message);
            }
        }
    }
}
