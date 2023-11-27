using EchoRelay.Core.Game;
using EchoRelay.Core.Server.Services.ServerDB;
using EchoRelay.Core.Server.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using EchoRelay.Core.Server;
using EchoRelay.Core.Server.Messages.ServerDB;

namespace EchoRelay.API.Controllers
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
