using EchoRelay.Core.Game;
using EchoRelay.Core.Server.Services.ServerDB;
using EchoRelay.Core.Server.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using EchoRelay.Core.Server;

namespace EchoRelay.API.Controllers
{
    [Route("serversList/")]
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
                return Ok(serverInfo);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
