using EchoRelay.Core.Game;
using EchoRelay.Core.Server.Storage;
using EchoRelay.Core.Server.Storage.Types;
using EchoRelay.Core.Utils;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;

namespace EchoRelay.API.Controllers
{
    [Route("accounts/")]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        static ServerStorage? Storage => ApiServer.Instance?.RelayServer.Storage;

        [HttpGet]
        public IActionResult Get(int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                if (pageNumber < 1)
                {
                    return BadRequest("Invalid page number");
                }

                if (Storage == null)
                {
                    return StatusCode((int)HttpStatusCode.InternalServerError, "Storage is null");
                }

                var accounts = Storage.Accounts.Keys();
                var skip = (pageNumber - 1) * pageSize;
                var page = accounts.Skip(skip).Take(pageSize);
                return Ok(page.Select(x => x.ToString()).ToArray());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            try
            {
                using var reader = new StreamReader(HttpContext.Request.Body);
                var body = await reader.ReadToEndAsync();
                if (string.IsNullOrEmpty(body))
                {
                    return BadRequest("Invalid request body");
                }

                if (Storage == null)
                {
                    return StatusCode((int)HttpStatusCode.InternalServerError, "Storage is null");
                }

                var account = JsonConvert.DeserializeObject<AccountResource>(body);
                if (account == null)
                {
                    return BadRequest("Invalid account");
                }

                var xPlatformId = XPlatformId.Parse(account.Profile.Server.XPlatformId);
                if (xPlatformId == null)
                {
                    return BadRequest("Invalid id");
                }

                Storage.Accounts.Set(account);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}")]
        public IActionResult AccountGet(string id)
        {
            try
            {
                if (Storage == null)
                {
                    return StatusCode((int)HttpStatusCode.InternalServerError, "Storage is null");
                }

                var xPlatformId = XPlatformId.Parse(id);
                if (xPlatformId == null)
                {
                    return BadRequest("Invalid id");
                }

                var account = Storage.Accounts.Get(xPlatformId);
                if (account == null)
                {
                    return NotFound("Account not found");
                }

                return Ok(account);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{id}")]
        public async Task<IActionResult> AccountPost(string id)
        {
            try
            {
                if (Storage == null)
                {
                    return StatusCode((int)HttpStatusCode.InternalServerError, "Storage is null");
                }

                var xPlatformId = XPlatformId.Parse(id);
                if (xPlatformId == null)
                {
                    return BadRequest("Invalid id");
                }

                using var reader = new StreamReader(HttpContext.Request.Body);
                var body = await reader.ReadToEndAsync();
                if (string.IsNullOrEmpty(body))
                {
                    return BadRequest("Invalid request body");
                }

                var account = Storage.Accounts.Get(xPlatformId);
                if (account == null)
                {
                    return NotFound("Account not found");
                }

                var newAccount = JsonConvert.DeserializeObject<JObject>(body);
                if (newAccount == null)
                {
                    return BadRequest("Invalid account");
                }

                var mergedAccount = JsonUtils.MergeObjects(account, newAccount);
                if (mergedAccount == null)
                {
                    return BadRequest("Invalid account");
                }

                if (mergedAccount.Profile.Server.XPlatformId != xPlatformId.ToString())
                {
                    return BadRequest("Invalid id");
                }

                Storage.Accounts.Set(account);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public IActionResult AccountDelete(string id)
        {
            try
            {
                if (Storage == null)
                {
                    return StatusCode((int)HttpStatusCode.InternalServerError, "Storage is null");
                }

                var xPlatformId = XPlatformId.Parse(id);
                if (xPlatformId == null)
                {
                    return BadRequest("Invalid id");
                }

                var account = Storage.Accounts.Get(xPlatformId);
                if (account == null)
                {
                    return NotFound("Account not found");
                }

                Storage.Accounts.Delete(xPlatformId);
                return Ok(account);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
