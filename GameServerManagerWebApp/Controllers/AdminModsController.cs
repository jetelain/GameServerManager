using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameServerManagerWebApp.Entites;
using GameServerManagerWebApp.Models;
using GameServerManagerWebApp.Services.Arma3Mods;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameServerManagerWebApp.Controllers
{
    public class AdminModsController : Controller
    {
        private readonly GameServerManagerContext _context;
        private readonly IArmaModManager _modManager;
        public AdminModsController(GameServerManagerContext context, IArmaModManager modManager)
        {
            _context = context;
            _modManager = modManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var infos = new List<HostModsViewModel>();
            var servers = await _context.HostServers.ToListAsync();
            foreach (var server in servers)
            {
                var info = new HostModsViewModel(server);
                info.IsInstalling = _modManager.IsInstalling(server);
                info.Mods = await _modManager.GetInstalledMods(server);
                info.LastInstall = _modManager.GetLastInstallResult(server);
                if (info.LastInstall != null && DateTime.UtcNow > info.LastInstall.Finished.AddHours(12))
                {
                    info.LastInstall = null;
                }
                infos.Add(info);
            }
            return View(infos);
        }

        [HttpGet]
        public async Task<IActionResult> Add(int id)
        {
            var server = await _context.HostServers.FindAsync(id);
            if (server == null)
            {
                return NotFound();
            }
            if (_modManager.IsInstalling(server))
            {
                return RedirectToAction(nameof(Index));
            }
            return View(server);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int id, [FromForm] string mods)
        {
            var server = await _context.HostServers.FindAsync(id);
            if (server == null)
            {
                return NotFound();
            }
            if (_modManager.IsInstalling(server))
            {
                return RedirectToAction(nameof(Index));
            }

            var list = (mods ?? string.Empty)
                .Split('\n')
                .Select(s => s.Trim())
                .Where(m => !string.IsNullOrEmpty(m))
                .Where(m => long.TryParse(m, out _))
                .ToList();

            var request = await _modManager.Add(server, list);

            return View("Added", request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateNow(int id)
        {
            var server = await _context.HostServers.FindAsync(id);
            if (server == null)
            {
                return NotFound();
            }
            await _modManager.RequestInstall(server);
            return RedirectToAction(nameof(Index));

        }

    }
}
