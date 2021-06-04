using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GameServerManagerWebApp.Entites;
using GameServerManagerWebApp.Services;
using Microsoft.Extensions.Logging;
using GameServerManagerWebApp.Models;
using System.IO;
using Renci.SshNet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http;
using System.Xml.Linq;

namespace GameServerManagerWebApp.Controllers
{
    [Authorize(Policy = "Admin")]
    public class AdminGameServersController : Controller
    {
        private readonly ILogger<AdminGameServersController> _logger;
        private readonly GameServerService _service;
        private readonly GameServerManagerContext _context;
        private readonly IHttpClientFactory _factory;

        public AdminGameServersController(ILogger<AdminGameServersController> logger, GameServerService service, GameServerManagerContext context, IHttpClientFactory factory)
        {
            _logger = logger;
            _service = service;
            _context = context;
            _factory = factory;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var processes = _service.GetRunningProcesses();

            var games = (await _context.GameServers.Include(g => g.HostServer).ToListAsync()).
                Select(game => _service.GetGameInfos(game, processes)).ToList();

            return View(games);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncConfig()
        {
            await _service.SyncAll();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(int id)
        {
            var gameServer = _context.GameServers
                .Include(s => s.SyncFiles)
                .Include(s => s.HostServer)
                .FirstOrDefault(g => g.GameServerID == id);
            if (gameServer == null)
            {
                return NotFound();
            }
            await _service.StartGameServer(User, await GetActiveConfiguration(gameServer));
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyConfig(int id)
        {
            var config = _context.GameServerConfigurations
                .Include(s => s.GameServer).ThenInclude(s => s.SyncFiles)
                .Include(s => s.GameServer).ThenInclude(s => s.HostServer)
                .Include(s => s.Files)
                .FirstOrDefault(g => g.GameServerConfigurationID == id);
            if (config == null)
            {
                return NotFound();
            }
            await _service.ApplyAllConfiguration(config);
            return RedirectToAction(nameof(Details), new { id = config.GameServerID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Stop(int id)
        {
            var gameServer = _context.GameServers
                .Include(s => s.HostServer)
                .FirstOrDefault(g => g.GameServerID == id);
            if (gameServer == null)
            {
                return NotFound();
            }
            await _service.StopGameServer(User, gameServer);
            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task<GameServerConfiguration> GetActiveConfiguration(GameServer gameServer)
        {
            var currentConfig = await _context.GameServerConfigurations
                .Include(g => g.Files)
                .Include(g => g.Modset)
                .Where(g => g.GameServerID == gameServer.GameServerID && g.IsActive)
                .FirstOrDefaultAsync();
            if (currentConfig == null)
            {
                currentConfig = new GameServerConfiguration() { 
                    IsActive = true, 
                    GameServer = gameServer, 
                    Label = "Default", 
                    Files = new List<GameConfigurationFile>(),
                    AccessToken = GameServerService.GenerateToken()
                };
                _context.Add(currentConfig);
                await _context.SaveChangesAsync();
            }
            return currentConfig;
        }


        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var gameServer = await _context.GameServers
                .Include(g => g.HostServer)
                .Where(g => g.GameServerID == id)
                .FirstOrDefaultAsync();
            if (gameServer == null)
            {
                return BadRequest();
            }

            var currentConfig = await GetActiveConfiguration(gameServer);

            var vm = new ServerInfosViewModel();
            vm.GameServer = gameServer;
            vm.GameServer.Configurations = await _context.GameServerConfigurations
                .Include(c => c.Modset)
                .Where(c => c.GameServerID == gameServer.GameServerID).ToListAsync();
            vm.GameServer.SyncFiles = await _context.GameServerSyncedFiles
                .Include(c => c.GameServer)
                .Where(c => c.GameServerID == gameServer.GameServerID).ToListAsync();
            vm.CurrentConfig = currentConfig;
            
            if (gameServer.HostServerID != null)
            {
                var gameConfig = _service.GetConfig(gameServer);
                using (var client = _service.GetSftpClient(gameConfig.Server))
                {
                    client.Connect();
                    if (!string.IsNullOrEmpty(gameConfig.MissionDirectory) && gameServer.Type == GameServerType.Arma3)
                    {
                        vm.MissionFiles = client.ListDirectory(gameConfig.MissionDirectory).Where(f => f.IsRegularFile).Select(f => Path.GetFileName(f.FullName)).Where(f => f.EndsWith(".pbo", StringComparison.OrdinalIgnoreCase)).ToList();
                        vm.MissionFiles.Sort();
                    }
                    await _service.SyncConfig(client, gameConfig, currentConfig);
                    client.Disconnect();
                }
                vm.Game = gameConfig;
                vm.Infos = _service.GetGameInfos(gameServer, _service.GetRunningProcesses(gameConfig.Server));
                vm.ConfigFiles = gameConfig.ConfigFiles.Select((f, i) => new ConfigFileInfos()
                {
                    Index = i,
                    Name = Path.GetFileName(f)
                }).ToList();
            }
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> UpdateSyncedFiles(int id)
        {
            var gameServer = _context.GameServers
                .Include(s => s.SyncFiles)
                .Include(s => s.HostServer)
                .FirstOrDefault(g => g.GameServerID == id);
            if (gameServer == null)
            {
                return NotFound();
            }
            await _service.UpdateSyncedFiles(gameServer);
            return RedirectToAction(nameof(Details), new { id });
        }

        private string GetLog(GameConfig game, SftpClient client)
        {
            if (game.ConsoleFile != null && client.Exists(game.ConsoleFile))
            {
                return client.ReadAllText(game.ConsoleFile);
            }
            if (game.ConsoleFileDirectory != null && game.ConsoleFilePrefix != null)
            {
                var files = client.ListDirectory(game.ConsoleFileDirectory);
                var file = files.Where(f => f.Name.StartsWith(game.ConsoleFilePrefix, StringComparison.OrdinalIgnoreCase)).OrderByDescending(f => f.LastWriteTimeUtc).FirstOrDefault();
                if (file != null)
                {
                    return client.ReadAllText(file.FullName);
                }
            }
            return string.Empty;
        }

        [HttpGet]
        public IActionResult FullLog(int id)
        {
            var gameServer = _context.GameServers.Include(g => g.HostServer).FirstOrDefault(g => g.GameServerID == id);
            if (gameServer == null || gameServer.HostServerID == null)
            {
                return NotFound();
            }
            var game = _service.GetConfig(gameServer);
            var vm = new ServerInfosViewModel();
            vm.GameServer = gameServer;
            vm.Infos = _service.GetGameInfos(gameServer, _service.GetRunningProcesses(game.Server));
            using (var client = _service.GetSftpClient(game.Server))
            {
                client.Connect();
                vm.Console = GetLog(game, client);
                client.Disconnect();
            }
            const int ConsoleMaxLength = 5_000_000;
            if (vm.Console != null && vm.Console.Length > ConsoleMaxLength)
            {
                vm.Console = vm.Console.Substring(vm.Console.Length - ConsoleMaxLength, ConsoleMaxLength);
            }
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Audit(int id)
        {
            var gameServer = _context.GameServers.Include(g => g.HostServer).FirstOrDefault(g => g.GameServerID == id);
            if (gameServer == null || gameServer.HostServerID == null)
            {
                return NotFound();
            }

            var logs = await _context.GameLogEvents.Where(e => e.GameServerID == id)
                .OrderByDescending(e => e.Timestamp)
                .Take(1000)
                .ToListAsync();

            var client = _factory.CreateClient();
            var names = new Dictionary<string, string>();
            foreach (var steamid in logs.Select(s => s.SteamId).Distinct())
            {
                if (!string.IsNullOrEmpty(steamid))
                {
                    var doc = XDocument.Parse(await client.GetStringAsync($"https://steamcommunity.com/profiles/{steamid}/?xml=1"));
                    names[steamid] = doc.Element("profile").Element("steamID").Value;
                }
            }
            ViewData["Server"] = gameServer;
            return View(logs.Select(l => new LogVM { Log = l, User = names.TryGetValue(l.SteamId, out string name) ? name : string.Empty }).ToList());
        }

        public IActionResult DownloadMission(int id, string mission)
        {
            var gameServer = _context.GameServers.Include(g => g.HostServer).FirstOrDefault(g => g.GameServerID == id);
            if (gameServer == null || gameServer.HostServerID == null)
            {
                return NotFound();
            }
            var game = _service.GetConfig(gameServer);

            var name = Path.GetFileName(mission);
            var sourceFile = game.MissionDirectory + "/" + name;

            var mem = new MemoryStream();
            using (var client = _service.GetSftpClient(game.Server))
            {
                client.Connect();
                client.DownloadFile(sourceFile, mem);
                client.Disconnect();
            }

            return new FileContentResult(mem.ToArray(), "application/octet-stream")
            {
                FileDownloadName = name
            };
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public IActionResult UploadMission(int id, IFormFile file)
        {
            var gameServer = _context.GameServers.Include(g => g.HostServer).FirstOrDefault(g => g.GameServerID == id);
            if (gameServer == null)
            {
                return BadRequest();
            }
            _service.UploadMissionFile(file, gameServer);
            return RedirectToAction(nameof(Details), new { id });
        }


        // GET: AdminGameServers/Create
        public IActionResult Create()
        {
            ViewData["HostServerID"] = new SelectList(_context.HostServers, "HostServerID", "Name");
            return View();
        }

        // POST: AdminGameServers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("GameServerID,HostServerID,Type,Label,Address,Port,UserName,BasePath,LastPollUtc")] GameServer gameServer)
        {
            if (ModelState.IsValid)
            {
                _context.Add(gameServer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["HostServerID"] = new SelectList(_context.HostServers, "HostServerID", "Name", gameServer.HostServerID);
            return View(gameServer);
        }

        // GET: AdminGameServers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gameServer = await _context.GameServers.FindAsync(id);
            if (gameServer == null)
            {
                return NotFound();
            }
            ViewData["HostServerID"] = new SelectList(_context.HostServers, "HostServerID", "Name", gameServer.HostServerID);
            return View(gameServer);
        }

        // POST: AdminGameServers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("GameServerID,HostServerID,Type,Label,Address,Port,UserName,BasePath,LastPollUtc")] GameServer gameServer)
        {
            if (id != gameServer.GameServerID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(gameServer);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GameServerExists(gameServer.GameServerID))
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
            ViewData["HostServerID"] = new SelectList(_context.HostServers, "HostServerID", "Name", gameServer.HostServerID);
            return View(gameServer);
        }

        // GET: AdminGameServers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gameServer = await _context.GameServers
                .Include(g => g.HostServer)
                .FirstOrDefaultAsync(m => m.GameServerID == id);
            if (gameServer == null)
            {
                return NotFound();
            }

            return View(gameServer);
        }

        // POST: AdminGameServers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var gameServer = await _context.GameServers.FindAsync(id);
            _context.GameServers.Remove(gameServer);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GameServerExists(int id)
        {
            return _context.GameServers.Any(e => e.GameServerID == id);
        }
    }
}
