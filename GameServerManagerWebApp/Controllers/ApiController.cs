using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using GameServerManagerWebApp.Entites;
using GameServerManagerWebApp.Models;
using GameServerManagerWebApp.Services.Arma3Mods;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameServerManagerWebApp.Controllers
{
    [Route("api")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly GameServerManagerContext _context;

        public ApiController(GameServerManagerContext context)
        {
            _context = context;
        }

        [Authorize(Policy = "ApiClient")]
        [HttpGet("configs")]
        public async Task<IActionResult> ConfigList()
        {
            var list = await _context.GameServerConfigurations
                .Include(g => g.GameServer).ThenInclude(g => g.HostServer)
                .Include(g => g.Modset)
                .ToListAsync();
            return Ok(list.Select(e => new ApiConfigSummary()
            {
                Id = e.GameServerConfigurationID,
                Label = e.Label,
                Href = Url.Action("Config", "Home", new { id = e.GameServerConfigurationID, t = e.AccessToken }, "https"),
                AccessToken = e.AccessToken
            }).ToList());
        }

        [HttpGet("configs/{id}")]
        public async Task<IActionResult> ConfigGet(int id, string t)
        {
            var e = await _context.GameServerConfigurations
                .Include(g => g.GameServer).ThenInclude(g => g.HostServer)
                .Include(g => g.Modset)
                .FirstOrDefaultAsync(m => m.GameServerConfigurationID == id && m.AccessToken == t);
            if (e == null)
            {
                return NotFound();
            }
            return Ok(new ApiConfig() {
                Id = e.GameServerConfigurationID,
                Label = e.Label,
                Href = Url.Action("Config", "Home", new { id = e.GameServerConfigurationID, t = e.AccessToken }, "https"),

                ModsetName = e.Modset?.Name,
                ModsetCount = e.Modset?.Count,
                ModsetHref = e.Modset == null ? null : Url.Action("DownloadModset", "Home", new { id = e.ModsetID, t = e.Modset.AccessToken }, "https"),
                ModsetId = e.Modset?.ModsetID,
                ModsetAccessToken = e.Modset?.AccessToken,

                ServerName = e.IsActive ? e.ServerName : string.Empty,
                ServerAddress = e.IsActive ? e.GameServer.Address : string.Empty,
                ServerPort = e.IsActive ? e.GameServer.Port : null,
                ServerPassword = e.IsActive ? e.ServerPassword : string.Empty,

                VoipServer = e.VoipServer,
                VoipChannel = e.VoipChannel,
                VoipPassword = e.VoipPassword,

                IsActive = e.IsActive,

                LastChangeUTC = e.LastChangeUTC
            });
        }

        [HttpGet("modsets/{id}")]
        public async Task<IActionResult> ModsetGet(int id, string t)
        {
            var modset = await _context.Modsets
                .FirstOrDefaultAsync(m => m.ModsetID == id && m.AccessToken == t);
            if (modset == null)
            {
                return NotFound();
            }
            var modsetMods = ModsetFileHelper.GetModsetEntries(XDocument.Parse(modset.DefinitionFile));

            return Ok(new ApiModset()
            {
                Id = modset.ModsetID,
                Name = modset.Name,
                Count = modset.Count,
                Href = Url.Action("DownloadModset", "Home", new { id = modset.ModsetID, t = modset.AccessToken }, "https"),
                LastChangeUTC = modset.LastUpdate,
                Mods = modsetMods
            });
        }
    } 
}
