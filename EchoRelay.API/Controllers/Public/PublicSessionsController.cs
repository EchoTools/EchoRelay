using EchoRelay.Core.Server.Services.ServerDB;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using EchoRelay.API.Public;
using EchoRelay.Core.Server.Messages.ServerDB;

namespace EchoRelay.API.Controllers.Public
{
    [Route("centralapi/sessionslist/")]
    [ApiController]
    public class PublicSessionsController : ControllerBase
    {
        static GameServerRegistry? Registry => ApiServer.Instance?.RelayServer.ServerDBService.Registry;

        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                if (Registry == null)
                {
                    return StatusCode((int)HttpStatusCode.InternalServerError, "Registry is null");
                }

                var publicSessions = new List<PublicSessionInfo>();
                var servers = Registry.RegisteredGameServers;
                foreach (var server in servers)
                {
                    var gameServer = server.Value;
                    if(gameServer.SessionLobbyType != ERGameServerStartSession.LobbyType.Private)
                    {
                        if (gameServer.SessionStarted)
                        {
                            publicSessions.Add(new PublicSessionInfo(gameServer));
                        }
                    }
                }

                return Ok(publicSessions);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
