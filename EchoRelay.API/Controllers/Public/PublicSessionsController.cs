using EchoRelay.Core.Game;
using EchoRelay.Core.Server.Services.ServerDB;
using EchoRelay.Core.Server.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using EchoRelay.Core.Server.Messages.ServerDB;

namespace EchoRelay.API.Controllers
{
    [Route("sessionsList/")]
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
                var servers = Registry.RegisteredGameServersBySessionId.Keys;
                foreach (var sessionId in servers)
                {
                    var gameServer = Registry.GetGameServer(sessionId);
                    if (gameServer != null)
                    {
                        if(gameServer.SessionLobbyType != ERGameServerStartSession.LobbyType.Private)
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
