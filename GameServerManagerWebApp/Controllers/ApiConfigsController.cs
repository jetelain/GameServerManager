using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameServerManagerWebApp.Entites;
using GameServerManagerWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameServerManagerWebApp.Controllers
{
    [Route("api/configs")]
    [ApiController]
    public class ApiConfigsController : ControllerBase
    {
        private readonly GameServerManagerContext _context;

        public ApiConfigsController(GameServerManagerContext context)
        {
            _context = context;
        }

        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> Get()
        {
            var list = await _context.GameServerConfigurations
                .Include(g => g.GameServer).ThenInclude(g => g.HostServer)
                .Include(g => g.Modset)
                .ToListAsync();
            return Ok(list.Select(e => new ApiConfig()
            {
                Id = e.GameServerConfigurationID,
                Label = e.Label,
                Href = Url.Action("Config", "Home", new { id = e.GameServerConfigurationID, t = e.AccessToken }, "https")
            }).ToList());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id, string t)
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

                ServerName = e.IsActive ? e.ServerName : string.Empty,
                ServerAddress = e.IsActive ? e.GameServer.Address : string.Empty,
                ServerPort = e.IsActive ? e.GameServer.Port : null,
                ServerPassword = e.IsActive ? e.ServerPassword : string.Empty,

                IsActive = e.IsActive
            });
        }

    }
}
