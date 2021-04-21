using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameServerManagerWebApp.Entites;
using GameServerManagerWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameServerManagerWebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly GameServerManagerContext _context;

        public HomeController(ILogger<HomeController> logger, GameServerManagerContext context)
        {
            _logger = logger;
            _context = context;
        }

        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> Index()
        {
            var gameServerManagerContext = _context.GameServerConfigurations.Include(g => g.GameServer).Include(g => g.Modset);
            return View(await gameServerManagerContext.ToListAsync());
        }

        public async Task<IActionResult> Config(int id, string t)
        {
            var gameServerConfiguration = await _context.GameServerConfigurations
                .Include(g => g.GameServer).ThenInclude(g => g.HostServer)
                .Include(g => g.Modset)
                .FirstOrDefaultAsync(m => m.GameServerConfigurationID == id && m.AccessToken == t);
            if (gameServerConfiguration == null)
            {
                return NotFound();
            }
            if (gameServerConfiguration.Modset != null && gameServerConfiguration.GameServer.Type == GameServerType.Arma3)
            {
                var mods = gameServerConfiguration.Modset.ConfigurationFile.Split(';');
                if (mods.Contains("@751965892"))
                {
                    ViewData["VoipPlugin"] = "ACRE2";
                    ViewData["VoipPluginHelp"] = "https://hq.1ergtd-reality.fr/wiki/doku.php?id=public:acre2";
                }
            }
            return View(gameServerConfiguration);
        }

        public async Task<IActionResult> DownloadModset(int id, string t)
        {
            var modset = await _context.Modsets
                .FirstOrDefaultAsync(m => m.ModsetID == id && m.AccessToken == t);
            if (modset == null)
            {
                return NotFound();
            }
            return File(Encoding.UTF8.GetBytes(modset.DefinitionFile), "application/octet-steam", $"Arma 3 Preset {modset.Name}.html");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
