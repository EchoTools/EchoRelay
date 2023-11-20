using EchoRelay.Core.Game;
using EchoRelay.Core.Server.Services.ServerDB;
using EchoRelay.Core.Server.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace EchoRelay.API.Controllers
{
    [Route("servers/")]
    [ApiController]
    public class ServersController : ControllerBase
    {
        static GameServerRegistry? Registry => ApiServer.Instance?.RelayServer.ServerDBService.Registry;

        [HttpGet]
        public IActionResult Get(int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                if (pageNumber < 1)
                {
                    return BadRequest("Invalid page number");
                }

                if (Registry == null)
                {
                    return StatusCode((int)HttpStatusCode.InternalServerError, "Registry is null");
                }

                var servers = Registry.RegisteredGameServers.Keys;
                var skip = (pageNumber - 1) * pageSize;
                var page = servers.Skip(skip).Take(pageSize);
                return Ok(page.Select(x => x.ToString()).ToArray());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{serverId}")]
        public IActionResult AccountGet(ulong serverId)
        {
            try
            {
                if (Registry == null)
                {
                    return StatusCode((int)HttpStatusCode.InternalServerError, "Registry is null");
                }

                var server = Registry.GetGameServer(serverId);
                if (server == null)
                {
                    return NotFound("Server not found");
                }

                var serverInfo = new ServerInfo(server);
                return Ok(serverInfo);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
