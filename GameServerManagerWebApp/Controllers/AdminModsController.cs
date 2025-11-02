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
        public async Task<IActionResult> Index(string sort = "")
        {
            var infos = new List<HostModsViewModel>();
            var servers = await _context.HostServers.ToListAsync();
            var modpacks = await _context.Modsets.ToListAsync();

            var usedMods = modpacks.SelectMany(m => m.ConfigurationFile.Split(';')).Select(m => m.Trim('@', ' ', '\r', '\n')).Where(m => !string.IsNullOrEmpty(m)).Distinct().ToHashSet();

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
            ViewBag.Sort = sort;
            ViewBag.UsedMods = usedMods;
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveEmptyMods(int id)
        {
            var server = await _context.HostServers.FindAsync(id);
            if (server == null)
            {
                return NotFound();
            }
            var mods = await _modManager.GetInstalledMods(server); ;
            var emptyMods = mods.Where(m => m.ModSize == 0).Select(m => m.ModSteamId).ToList();
            if (emptyMods.Count > 0)
            {
                await _modManager.RemoveFromList(server, emptyMods);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveDuplicates(int id)
        {
            var server = await _context.HostServers.FindAsync(id);
            if (server == null)
            {
                return NotFound();
            }
            await _modManager.RemoveDuplicates(server);
            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id, long modSteamId)
        {
            var server = await _context.HostServers.FindAsync(id);
            if (server == null)
            {
                return NotFound();
            }
            await _modManager.Uninstall(server, new [] { modSteamId });
            return RedirectToAction(nameof(Index));
        }
    }
}
