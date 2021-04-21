using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using GameServerManagerWebApp.Entites;
using GameServerManagerWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Renci.SshNet;

namespace GameServerManagerWebApp.Controllers
{
    [Authorize(Policy = "Admin")]
    public class AdminGameServerConfigurationsController : Controller
    {
        private readonly GameServerManagerContext _context;
        private readonly GameServerService _service;

        public AdminGameServerConfigurationsController(GameServerManagerContext context, GameServerService service)
        {
            _context = context;
            _service = service;
        }

        // GET: AdminGameServerConfigurations/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gameServerConfiguration = await _context.GameServerConfigurations
                .Include(g => g.GameServer).ThenInclude(g => g.HostServer)
                .Include(g => g.Modset)
                .Include(g => g.Files)
                .FirstOrDefaultAsync(m => m.GameServerConfigurationID == id);
            if (gameServerConfiguration == null)
            {
                return NotFound();
            }

            if (gameServerConfiguration.IsActive && gameServerConfiguration.GameServer.HostServerID != null)
            {
                var gameConfig = _service.GetConfig(gameServerConfiguration.GameServer);
                using (var client = _service.GetSftpClient(gameConfig.Server))
                {
                    client.Connect();
                    await _service.SyncConfig(client, gameConfig, gameServerConfiguration);
                    client.Disconnect();
                }
            }

            return View(gameServerConfiguration);
        }

        // GET: AdminGameServerConfigurations/Create
        public async Task<IActionResult> Copy(int id, int? gameServerID)
        {
            var gameServerConfiguration = await _context.GameServerConfigurations
                .Include(g => g.GameServer).ThenInclude(g => g.HostServer)
                .FirstOrDefaultAsync(m => m.GameServerConfigurationID == id);
            if (gameServerConfiguration == null)
            {
                return NotFound();
            }
            if (gameServerID != null)
            {
                gameServerConfiguration.GameServerID = gameServerID.Value; 
            }

            ViewData["GameServerID"] = new SelectList(_context.GameServers, "GameServerID", "Label", gameServerConfiguration.GameServerID);
            PrepareDropdownLists(gameServerConfiguration);
            return View(gameServerConfiguration);
        }

        // POST: AdminGameServerConfigurations/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Copy([Bind("GameServerConfigurationID,GameServerID,ServerName,ServerPassword,ServerMission,VoipServer,VoipChannel,VoipPassword,EventHref,EventImage,ModsetID,Label")] GameServerConfiguration gsc, IFormFile modset, IFormFile mission)
        {
            var source = await _context.GameServerConfigurations.FirstOrDefaultAsync(m => m.GameServerConfigurationID == gsc.GameServerConfigurationID);

            gsc.GameServer = await _context.GameServers
                .Include(g => g.HostServer)
                .FirstOrDefaultAsync(m => m.GameServerID == gsc.GameServerID);

            await HandleFiles(gsc, modset, mission);

            if (ModelState.IsValid)
            {
                var copy = new GameServerConfiguration();
                copy.AccessToken = GameServerService.GenerateToken();
                copy.GameServerID = gsc.GameServerID;
                copy.ServerName = gsc.ServerName;
                copy.ServerPassword = gsc.ServerPassword;
                copy.ServerMission = gsc.ServerMission;
                copy.VoipServer = gsc.VoipServer;
                copy.VoipChannel = gsc.VoipChannel;
                copy.VoipPassword = gsc.VoipPassword;
                copy.EventHref = gsc.EventHref;
                copy.EventImage = gsc.EventImage;
                copy.ModsetID = gsc.ModsetID;
                copy.Label = gsc.Label;
                copy.Files = (await _context.GameConfigurationFiles.Where(f => f.GameServerConfigurationID == gsc.GameServerConfigurationID).ToListAsync())
                    .Select(f => new GameConfigurationFile()
                    {
                        Configuration = copy,
                        Content = f.Content,
                        Path = f.Path
                    }).ToList();
                await ApplyEditConfig(copy.Files, source, gsc);
                _context.Add(copy);
                _context.AddRange(copy.Files);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(AdminGameServersController.Details), "AdminGameServers", new { id = gsc.GameServerID });
            }
            ViewData["GameServerID"] = new SelectList(_context.GameServers, "GameServerID", "Label", gsc.GameServerID);
            PrepareDropdownLists(gsc);
            return View(gsc);
        }

        private async Task HandleFiles(GameServerConfiguration gsc, IFormFile modsetFile, IFormFile missionFile)
        {
            if (missionFile != null)
            {
                gsc.ServerMission = _service.UploadMissionFile(missionFile, gsc.GameServer);
            }

            if (modsetFile != null)
            {
                var modset = new Modset();
                var errors = await _service.ParseArma3Modset(modset, modsetFile);
                if (!string.IsNullOrEmpty(errors))
                {
                    ModelState.AddModelError("ModsetID", errors);
                    return;
                }
                await GetOrCreateModset(gsc, modset);
            }

            if (gsc.ModsetID != null)
            {
                var actualModset = await _context.Modsets.FindAsync(gsc.ModsetID.Value);
                var errors = _service.ValidateModsetOnServer(actualModset, gsc.GameServer);
                if (!string.IsNullOrEmpty(errors))
                {
                    ModelState.AddModelError("ModsetID", errors);
                    return;
                }
            }
        }

        private async Task GetOrCreateModset(GameServerConfiguration gsc, Modset modset)
        {
            var existing = await _context.Modsets.FirstOrDefaultAsync(m => m.Name == modset.Name && m.ConfigurationFile == modset.ConfigurationFile);
            if (existing != null)
            {
                gsc.Modset = existing;
                gsc.ModsetID = existing.ModsetID;
            }
            else
            {
                modset.GameType = GameServerType.Arma3;
                modset.AccessToken = GameServerService.GenerateToken();
                modset.Label = modset.Name;
                _context.Add(modset);
                await _context.SaveChangesAsync();

                gsc.Modset = modset;
                gsc.ModsetID = modset.ModsetID;
            }
        }

        private async Task<IEnumerable<GameConfigurationFile>> ApplyEditConfig(List<GameConfigurationFile> files, GameServerConfiguration oldValue, GameServerConfiguration newValue)
        {
            var updated = new HashSet<GameConfigurationFile>();
            var serverCfg = files.First(f => f.Path == "server.cfg");
            if (oldValue.ServerName != newValue.ServerName)
            {
                updated.Add(ApplyUpdate(serverCfg, GameServerService.LabelRegex, newValue.ServerName));
            }
            if (oldValue.ServerPassword != newValue.ServerPassword)
            {
                updated.Add(ApplyUpdate(serverCfg, GameServerService.PasswordRegex, newValue.ServerPassword));
            }
            if (oldValue.ServerMission != newValue.ServerMission)
            {
                updated.Add(ApplyUpdate(serverCfg, GameServerService.MissionRegex, newValue.ServerMission));
            }
            if (oldValue.ModsetID != newValue.ModsetID)
            {
                var mods = files.First(f => f.Path == "mods.txt");
                mods.Content = (await _context.Modsets.FindAsync(newValue.ModsetID)).ConfigurationFile;
                updated.Add(mods);
            }
            return updated;
        }

        private GameConfigurationFile ApplyUpdate(GameConfigurationFile gameConfigurationFile, Regex regex, string value)
        {
            var match = regex.Match(gameConfigurationFile.Content);
            if (match.Success)
            {
                gameConfigurationFile.Content = 
                    gameConfigurationFile.Content.Substring(0, match.Groups[1].Index) + value 
                    + gameConfigurationFile.Content.Substring(match.Groups[1].Index + match.Groups[1].Length);
            }
            return gameConfigurationFile;
        }

        // GET: AdminGameServerConfigurations/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var gameServerConfiguration = await _context.GameServerConfigurations
                .Include(g => g.GameServer).ThenInclude(g => g.HostServer)
                .FirstOrDefaultAsync(m => m.GameServerConfigurationID == id);
            if (gameServerConfiguration == null)
            {
                return NotFound();
            }
            return View(gameServerConfiguration);
        }

        // POST: AdminGameServerConfigurations/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("GameServerConfigurationID,VoipServer,VoipChannel,VoipPassword,EventHref,EventImage,Label")] GameServerConfiguration gsc)
        {
            if (id != gsc.GameServerConfigurationID)
            {
                return NotFound();
            }

            var existing = await _context.GameServerConfigurations
                .Include(g => g.GameServer).ThenInclude(g => g.HostServer)
                .FirstOrDefaultAsync(m => m.GameServerConfigurationID == id);
            if (existing == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    existing.VoipServer = gsc.VoipServer;
                    existing.VoipChannel = gsc.VoipChannel;
                    existing.VoipPassword = gsc.VoipPassword;
                    existing.EventHref = gsc.EventHref;
                    existing.EventImage = gsc.EventImage;
                    existing.Label = gsc.Label;
                    _context.Update(existing);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GameServerConfigurationExists(existing.GameServerConfigurationID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Details), new { id });
            }
            return View(existing);
        }

        // GET: AdminGameServerConfigurations/Edit/5
        public async Task<IActionResult> EditConfig(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var gameServerConfiguration = await _context.GameServerConfigurations
                .Include(g => g.GameServer).ThenInclude(g => g.HostServer)
                .FirstOrDefaultAsync(m => m.GameServerConfigurationID == id);
            if (gameServerConfiguration == null)
            {
                return NotFound();
            }
            PrepareDropdownLists(gameServerConfiguration);
            return View(gameServerConfiguration);
        }

        private void PrepareDropdownLists(GameServerConfiguration gameServerConfiguration)
        {
            ViewData["ModsetID"] = new SelectList(_context.Modsets, "ModsetID", "Name", gameServerConfiguration?.ModsetID);
            if (gameServerConfiguration.GameServer.HostServerID != null)
            {
                var gameConfig = _service.GetConfig(gameServerConfiguration.GameServer);

                if (!string.IsNullOrEmpty(gameConfig.MissionDirectory) && gameServerConfiguration.GameServer.Type == GameServerType.Arma3)
                {
                    using (var client = _service.GetSftpClient(gameConfig.Server))
                    {
                        client.Connect();
                        var missions = client.ListDirectory(gameConfig.MissionDirectory).Where(f => f.IsRegularFile).Select(f => Path.GetFileName(f.FullName)).Where(f => f.EndsWith(".pbo", StringComparison.OrdinalIgnoreCase)).Select(f => Path.GetFileNameWithoutExtension(f)).ToList();
                        missions.Sort();
                        ViewData["ServerMission"] = missions.Select(n => new SelectListItem(n, n)).ToList();
                        client.Disconnect();
                    }
                }
            }
        }

        // POST: AdminGameServerConfigurations/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditConfig(int id, [Bind("GameServerConfigurationID,ServerName,ServerPassword,ServerMission,ModsetID")] GameServerConfiguration gsc, IFormFile modset, IFormFile mission)
        {
            if (id != gsc.GameServerConfigurationID)
            {
                return NotFound();
            }

            var existing = await _context.GameServerConfigurations
                .Include(g => g.GameServer).ThenInclude(g => g.HostServer)
                .Include(g => g.Files)
                .FirstOrDefaultAsync(m => m.GameServerConfigurationID == id);
            if (existing == null)
            {
                return NotFound();
            }

            gsc.GameServer = existing.GameServer;

            await HandleFiles(gsc, modset, mission);

            if (ModelState.IsValid)
            {
                try
                {
                    var files = await ApplyEditConfig(existing.Files, existing, gsc);

                    existing.ServerName = gsc.ServerName;
                    existing.ServerPassword = gsc.ServerPassword;
                    existing.ServerMission = gsc.ServerMission;
                    existing.ModsetID = gsc.ModsetID;

                    if (existing.GameServer.HostServerID != null)
                    {
                        foreach (var file in files)
                        {
                            _context.Update(file);
                        }

                        if (existing.IsActive)
                        {
                            using (var client = _service.GetSftpClient(existing.GameServer.HostServer))
                            {
                                client.Connect();
                                foreach (var file in files)
                                {
                                    _service.WriteAllText(client, _service.GetFileFullPath(_service.GetConfig(file.Configuration.GameServer), file.Path), file.Content);
                                }
                                client.Disconnect();
                            }
                        }
                    }

                    _context.Update(existing);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GameServerConfigurationExists(existing.GameServerConfigurationID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Details), new { id });
            }
            return View(existing);
        }

        // GET: AdminGameServerConfigurations/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gameServerConfiguration = await _context.GameServerConfigurations
                .Include(g => g.GameServer)
                .Include(g => g.Modset)
                .FirstOrDefaultAsync(m => m.GameServerConfigurationID == id);
            if (gameServerConfiguration == null)
            {
                return NotFound();
            }

            return View(gameServerConfiguration);
        }

        // POST: AdminGameServerConfigurations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var gameServerConfiguration = await _context.GameServerConfigurations.FindAsync(id);
            if (gameServerConfiguration.IsActive)
            {
                return RedirectToAction(nameof(Index));
            }
            _context.GameServerConfigurations.Remove(gameServerConfiguration);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GameServerConfigurationExists(int id)
        {
            return _context.GameServerConfigurations.Any(e => e.GameServerConfigurationID == id);
        }

        [HttpGet]
        public async Task<IActionResult> EditFileAsync(int id)
        {
            var file = await _context.GameConfigurationFiles
                .Include(g => g.Configuration).ThenInclude(g => g.GameServer).ThenInclude(e => e.HostServer)
                .Include(g => g.Configuration).ThenInclude(g => g.Files)
                .FirstOrDefaultAsync(m => m.GameConfigurationFileID == id);

            if (file == null || file.Configuration.GameServer.HostServerID == null)
            {
                return NotFound();
            }

            if (file.Configuration.IsActive)
            {
                using (var client = _service.GetSftpClient(file.Configuration.GameServer.HostServer))
                {
                    var fullpath = _service.GetFileFullPath(_service.GetConfig(file.Configuration.GameServer), file.Path);
                    client.Connect();
                    if (client.Exists(fullpath))
                    {
                        file.Content = client.ReadAllText(fullpath);
                    }
                    client.Disconnect();
                }
            }

            return View(file);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> EditFile(int id, GameConfigurationFile editFile)
        {
            var file = await _context.GameConfigurationFiles
                .Include(g => g.Configuration).ThenInclude(g => g.GameServer).ThenInclude(e => e.HostServer)
                .Include(g => g.Configuration).ThenInclude(g => g.Files)
                .FirstOrDefaultAsync(m => m.GameConfigurationFileID == id);

            if (file == null || file.Configuration.GameServer.HostServerID == null)
            {
                return NotFound();
            }

            file.Content = editFile.Content;

            _context.GameConfigurationFiles.Update(file);

            await _service.UpdateComputedProperties(file.Configuration);
            _context.GameServerConfigurations.Update(file.Configuration);
            await _context.SaveChangesAsync();

            if (file.Configuration.IsActive)
            {
                using (var client = _service.GetSftpClient(file.Configuration.GameServer.HostServer))
                {
                    client.Connect();
                    _service.WriteAllText(client, _service.GetFileFullPath(_service.GetConfig(file.Configuration.GameServer), file.Path), file.Content);
                    client.Disconnect();
                }
            }

            return RedirectToAction(nameof(Details), new { id = file.GameServerConfigurationID });
        }
    }
}
