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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Renci.SshNet;

namespace GameServerManagerWebApp.Controllers
{
    //[Authorize(Policy = "Admin")]
    public class AdminModsetsController : Controller
    {
        private readonly GameServerManagerContext _context;

        public AdminModsetsController(GameServerManagerContext context)
        {
            _context = context;
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

            var Modset = await _context.Modsets
                .FirstOrDefaultAsync(m => m.ModsetID == id);
            if (Modset == null)
            {
                return NotFound();
            }

            return View(Modset);
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
            if (ModelState.IsValid)
            {
                modset.GameType = GameServerType.Arma3;
                modset.AccessToken = GameServerService.GenerateToken();
                await ParseArma3Modset(modset, data);
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
                        await ParseArma3Modset(modset, data);
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

        private async Task<bool> ParseArma3Modset(Modset modset, IFormFile data)
        {
            using (var reader = new StreamReader(data.OpenReadStream(), Encoding.UTF8))
            {
                modset.DefinitionFile = await reader.ReadToEndAsync();
            }
            modset.GameType = GameServerType.Arma3;
            modset.LastUpdate = DateTime.Now;
            var doc = XDocument.Parse(modset.DefinitionFile);
            modset.Count = doc.Descendants("tr").Attributes("data-type").Where(a => a.Value == "ModContainer").Count();
            modset.Name = doc.Descendants("meta").Where(m => m.Attribute("name").Value == "arma:PresetName").Select(m => m.Attribute("content").Value).FirstOrDefault();
            var analyze = AnalyseModsetFile(null, doc, null);
            if (!analyze.Any(m => m.IsOK))
            {
                return false;
            }
            modset.ConfigurationFile = string.Join(";", analyze.Select(m => "@" + m.Id));
            return true;
        }
        internal static List<SetupArma3Mod> AnalyseModsetFile(GameConfig game, XDocument doc, SftpClient client)
        {
            var steamPrefix = "http://steamcommunity.com/sharedfiles/filedetails/?id=";
            var mods = new List<SetupArma3Mod>();
            foreach (var mod in doc.Descendants("tr").Attributes("data-type").Where(a => a.Value == "ModContainer"))
            {
                var name = mod.Parent.Descendants("td").Attributes("data-type").Where(a => a.Value == "DisplayName").FirstOrDefault()?.Parent?.FirstNode?.ToString();
                var href = mod.Parent.Descendants("a").Attributes("href").FirstOrDefault()?.Value;
                if (!string.IsNullOrEmpty(href) && href.StartsWith(steamPrefix))
                {
                    var modSteamId = href.Substring(steamPrefix.Length);
                    if (client == null || client.Exists(game.GameBaseDirectory + "/@" + modSteamId))
                    {
                        mods.Add(new SetupArma3Mod() { Id = modSteamId, Name = name, Href = href, IsOK = true });
                    }
                    else
                    {
                        mods.Add(new SetupArma3Mod() { Id = modSteamId, Name = name, Href = href, IsOK = false, Message = "Mod non disponible au catalogue" });
                    }
                }
                else
                {
                    mods.Add(new SetupArma3Mod() { Name = name, Href = href, IsOK = false, Message = "Mod non disponible sur Steam" });
                }
            }
            return mods;
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
