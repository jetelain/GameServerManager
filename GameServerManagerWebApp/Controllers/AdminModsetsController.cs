using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using GameServerManagerWebApp.Entites;
using GameServerManagerWebApp.Models;
using GameServerManagerWebApp.Services;
using GameServerManagerWebApp.Services.Arma3Mods;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Renci.SshNet;

namespace GameServerManagerWebApp.Controllers
{
    [Authorize(Policy = "Admin")]
    public class AdminModsetsController : Controller
    {
        private readonly GameServerManagerContext _context;
        private readonly GameServerService _service;
        private readonly IArmaModManager _modManager;

        public AdminModsetsController(GameServerManagerContext context, GameServerService service, IArmaModManager modManager)
        {
            _context = context;
            _service = service;
            _modManager = modManager;
        }

        // GET: Modsets
        public async Task<IActionResult> Index()
        {
            return View(await _context.Modsets.ToListAsync());
        }

        // GET: Modsets/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var modset = await _context.Modsets
                .FirstOrDefaultAsync(m => m.ModsetID == id);
            if (modset == null)
            {
                return NotFound();
            }

            var servers = await _context.HostServers.ToListAsync();

            modset.Servers = new List<ModesetGameServerMods>();

            foreach (var server in servers)
            {
                // ModsetModViewModel
                var serverMods = await _modManager.GetInstalledMods(server);
                var modsetMods = ModsetFileHelper.GetModsetEntries(XDocument.Parse(modset.DefinitionFile));


                modset.Servers.Add(new ModesetGameServerMods()
                {
                    HostServer = server,
                    Mods = modsetMods.Select(m => new ModsetModViewModel()
                    {
                        Name = m.Name,
                        SteamId = m.SteamId,
                        Href = m.Href,
                        Installed = serverMods.FirstOrDefault(sm => sm.ModSteamId == m.SteamId)
                    }).ToList()
                });
            }
            return View(modset);
        }

        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            var Modset = await _context.Modsets
                .FirstOrDefaultAsync(m => m.ModsetID == id);
            if (Modset == null)
            {
                return NotFound();
            }
            return File(Encoding.UTF8.GetBytes(Modset.DefinitionFile), "application/octet-steam", $"Modset_{Modset.Name}.html");
        }

        // GET: Modsets/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Modsets/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Label")] Modset modset, IFormFile data)
        {
            await _service.ParseArma3Modset(modset, data);

            if (ModelState.IsValid)
            {
                modset.GameType = GameServerType.Arma3;
                modset.AccessToken = GameServerService.GenerateToken();
                modset.Label = modset.Label ?? modset.Name;
                _context.Add(modset);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(modset);
        }

        // GET: Modsets/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var Modset = await _context.Modsets.FindAsync(id);
            if (Modset == null)
            {
                return NotFound();
            }
            return View(Modset);
        }

        // POST: Modsets/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ModsetID,Label")] Modset modset, IFormFile data)
        {
            if (id != modset.ModsetID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var previous = await _context.Modsets.AsNoTracking().FirstOrDefaultAsync(m => m.ModsetID == id);
                    modset.AccessToken = previous.AccessToken;

                    if (data == null)
                    {
                        modset.DefinitionFile = previous.DefinitionFile;
                        modset.Count = previous.Count;
                        modset.ConfigurationFile = previous.ConfigurationFile;
                        modset.LastUpdate = previous.LastUpdate;
                        modset.Name = previous.Name;
                    }
                    else
                    {
                        await _service.ParseArma3Modset(modset, data);
                    }
                    _context.Update(modset);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ModsetExists(modset.ModsetID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(modset);
        }

        // GET: Modsets/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var Modset = await _context.Modsets
                .FirstOrDefaultAsync(m => m.ModsetID == id);
            if (Modset == null)
            {
                return NotFound();
            }

            return View(Modset);
        }

        // POST: Modsets/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var Modset = await _context.Modsets.FindAsync(id);
            _context.Modsets.Remove(Modset);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ModsetExists(int id)
        {
            return _context.Modsets.Any(e => e.ModsetID == id);
        }
    }
}
