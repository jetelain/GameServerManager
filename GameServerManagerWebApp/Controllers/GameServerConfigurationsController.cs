//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Text.RegularExpressions;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Xml.Linq;
//using GameServerManagerWebApp.Entites;
//using GameServerManagerWebApp.Models;
//using GameServerManagerWebApp.Services;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Caching.Memory;
//using Microsoft.Extensions.Logging;
//using Renci.SshNet;

//namespace GameServerManagerWebApp.Controllers
//{
//    [Authorize(Policy = "Admin")]
//    public class GameServerConfigurationsController : Controller
//    {
//        private readonly ILogger<GameServersController> _logger;
//        private readonly GameServerService _service;
//        private readonly GameServerManagerContext _context;
//        private readonly IMemoryCache _cache;

//        private static readonly Regex PasswordRegex = new Regex(@"password\s*=\s*""(.*)""", RegexOptions.IgnoreCase);
//        private static readonly Regex LabelRegex = new Regex(@"hostname\s*=\s*""(.*)""", RegexOptions.IgnoreCase);

//        public GameServerConfigurationsController(ILogger<GameServersController> logger, GameServerService service, GameServerManagerContext context, IMemoryCache cache)
//        {
//            _logger = logger;
//            _service = service;
//            _context = context;
//            _cache = cache;
//        }


//        private static readonly Regex NameRegex = new Regex(@"name\s+=\s""(.*)""", RegexOptions.Compiled);
//        private static readonly Regex IdRegex = new Regex(@"publishedid\s+=\s([0-9]+)", RegexOptions.Compiled);
//        private static readonly Regex IsIdRegex = new Regex(@"^[0-9]+$", RegexOptions.Compiled);


//        [HttpGet]
//        public async Task<IActionResult> SetupArma3Modset(string id)
//        {
//            var game = _service.Games.FirstOrDefault(g => g.Name == id);
//            if (game == null)
//            {
//                return NotFound();
//            }
//            var vm = new SetupArma3ModsetVM();
//            await SyncMetadata();
//            await PrepareModsetVM(game, vm);
//            return View(vm);
//        }

//        private async Task PrepareModsetVM(GameConfig game, SetupArma3ModsetVM vm)
//        {
//            vm.Game = game;
//            vm.Modsets = await _context.Modsets.ToListAsync();
//            var dbEntry = await _context.EventGameServers.FirstOrDefaultAsync(e => e.Name == game.Name);
//            vm.ModsetID = dbEntry?.CurrentModsetID;
//        }

//        [ValidateAntiForgeryToken]
//        [HttpPost]
//        public async Task<IActionResult> SetupArma3Modset(string id, IFormFile file, int? ModsetID)
//        {
//            var game = _service.Games.FirstOrDefault(g => g.Name == id);
//            if (game == null)
//            {
//                return NotFound();
//            }
//            var vm = new SetupArma3ModsetVM();

//            await PrepareModsetVM(game, vm);

//            XDocument doc;
//            if (ModsetID != null)
//            {
//                var Modset = await _context.Modsets.FindAsync(ModsetID);
//                if (Modset == null)
//                {
//                    return View(vm);
//                }
//                doc = XDocument.Parse(Modset.HtmlContent);
//            }
//            else if (file != null)
//            {
//                doc = await ParseModset(file);
//            }
//            else
//            {
//                return View(vm);
//            }

//            using (var client = _service.GetSftpClient(game.Server))
//            {
//                client.ConnectionInfo.Timeout = TimeSpan.FromMilliseconds(1000);
//                client.Connect();

//                List<SetupArma3Mod> mods = AnalyseModsetFile(game, doc, client);
//                vm.Mods = mods;
//                if (mods.All(m => m.IsOK))
//                {
//                    vm.IsSetupOK = true;
//                    var modsText = string.Join(";", mods.Select(m => "@" + m.Id));
//                    UploadFile(client, modsText, game.GameBaseDirectory + "/mods.txt");
//                    UploadFile(client, modsText, game.SyncModsDirectory + "/mods.txt");
//                }
//                else
//                {
//                    vm.IsSetupFailed = true;
//                }
//                client.Disconnect();
//            }

//            await SyncMetadata();

//            return View(vm);
//        }

//        private static void UploadFile(SftpClient client, string content, string target)
//        {
//            BackupIfExists(target, client);
//            client.UploadFile(new MemoryStream(Encoding.UTF8.GetBytes(content)), target, true);
//        }

//        private static async Task<XDocument> ParseModset(IFormFile file)
//        {
//            string modsDefinition;
//            using (var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8))
//            {
//                modsDefinition = await reader.ReadToEndAsync();
//            }
//            return XDocument.Parse(modsDefinition);
//        }

//        internal static List<SetupArma3Mod> AnalyseModsetFile(GameConfig game, XDocument doc, SftpClient client)
//        {
//            var steamPrefix = "http://steamcommunity.com/sharedfiles/filedetails/?id=";
//            var mods = new List<SetupArma3Mod>();
//            foreach (var mod in doc.Descendants("tr").Attributes("data-type").Where(a => a.Value == "ModContainer"))
//            {
//                var name = mod.Parent.Descendants("td").Attributes("data-type").Where(a => a.Value == "DisplayName").FirstOrDefault()?.Parent?.FirstNode?.ToString();
//                var href = mod.Parent.Descendants("a").Attributes("href").FirstOrDefault()?.Value;
//                if (!string.IsNullOrEmpty(href) && href.StartsWith(steamPrefix))
//                {
//                    var modSteamId = href.Substring(steamPrefix.Length);
//                    if (client == null || client.Exists(game.GameBaseDirectory + "/@" + modSteamId))
//                    {
//                        mods.Add(new SetupArma3Mod() { Id = modSteamId, Name = name, Href = href, IsOK = true });
//                    }
//                    else
//                    {
//                        mods.Add(new SetupArma3Mod() { Id = modSteamId, Name = name, Href = href, IsOK = false, Message = "Mod non disponible au catalogue" });
//                    }
//                }
//                else
//                {
//                    mods.Add(new SetupArma3Mod() { Name = name, Href = href, IsOK = false, Message = "Mod non disponible sur Steam" });
//                }
//            }
//            return mods;
//        }

//        [HttpGet]
//        public IActionResult Arma3ModsCatalog(string server)
//        {
//            if ( string.IsNullOrEmpty(server))
//            {
//                server = _service.Games.First().Server;
//            }

//            var vm = new Arma3ModsCatalogVM();
//            vm.Server = server;
//            vm.Mods = new List<ModInfoVM>();
//            using (var client = _service.GetSftpClient(server))
//            {
//                client.ConnectionInfo.Timeout = TimeSpan.FromMilliseconds(1000);
//                client.Connect();
//                foreach(var modDir in client.ListDirectory("/home/arma3-mods/steamapps/workshop/content/107410"))
//                {
//                    if (!modDir.Name.StartsWith(".", StringComparison.OrdinalIgnoreCase))
//                    {
//                        vm.Mods.Add(GetModInfos(client, "/home/arma3-mods/steamapps/workshop/content/107410", modDir.Name));
//                    }
//                }
//                client.Disconnect();
//            }
//            vm.Mods.Sort((a, b) => a.Name.CompareTo(b.Name));
//            return View(vm);
//        }

//        [HttpGet]
//        public async Task<IActionResult> UpdateGtdRegimentConfig(string id)
//        {
//            var game = _service.Games.FirstOrDefault(g => g.Name == id);
//            if (game == null || game.GtdRegimentConfig == null)
//            {
//                return BadRequest();
//            }
//            var bytes = await ArmaController.GetRegimentConfig(_context);
//            var mem = new MemoryStream(bytes);
//            using (var client = _service.GetSftpClient(game.Server))
//            {
//                client.Connect();
//                client.UploadFile(mem, game.GtdRegimentConfig, true);
//                client.Disconnect();
//            }
//            return View(game);
//        }


//        [HttpGet]
//        public IActionResult Start(string id)
//        {
//            var game = _service.Games.FirstOrDefault(g => g.Name == id);
//            if (game == null)
//            {
//                return BadRequest();
//            }
//            using (var client = _service.GetClient(game.Server))
//            {
//                client.Connect();
//                var result = client.RunCommand(game.StartCmd);
//                Thread.Sleep(100);
//                client.Disconnect();
//            }
//            return RedirectToAction(nameof(Index));
//        }

//        [HttpGet]
//        public IActionResult Stop(string id)
//        {
//            var game = _service.Games.FirstOrDefault(g => g.Name == id);
//            if (game == null)
//            {
//                return BadRequest();
//            }
//            using (var client = _service.GetClient(game.Server))
//            {
//                client.Connect();
//                var result = client.RunCommand(game.StopCmd);
//                Thread.Sleep(100);
//                client.Disconnect();
//            }
//            return RedirectToAction(nameof(Index));
//        }

//        [HttpGet]
//        public async Task<IActionResult> ServerInfos(int id)
//        {
//            var gameServer = await _context.GameServers
//                .Include(g => g.HostServer)
//                .Where(g => g.GameServerID == id)
//                .FirstOrDefaultAsync();
//            if (gameServer == null)
//            {
//                return BadRequest();
//            }

//            var currentConfig = await _context.GameServerConfigurations
//                .Include(g => g.Files)
//                .Where(g => g.GameServerID == id && g.IsActive)
//                .FirstOrDefaultAsync();

//            if (currentConfig == null)
//            {
//                currentConfig = new GameServerConfiguration() { IsActive = true, GameServer = gameServer, Label = "Default", Files = new List<GameConfigurationFile>() };
//                _context.Add(currentConfig);
//                await _context.SaveChangesAsync();
//            }

//            var vm = new ServerInfosViewModel();
//            var gameConfig = _service.GetConfig(gameServer);

//            using (var client = _service.GetSftpClient(gameConfig.Server))
//            {
//                client.Connect();
//                if (!string.IsNullOrEmpty(gameConfig.MissionDirectory))
//                {
//                    vm.MissionFiles = client.ListDirectory(gameConfig.MissionDirectory).Where(f => f.IsRegularFile).Select(f => Path.GetFileName(f.FullName)).ToList();
//                    vm.MissionFiles.Sort();
//                }
//                await SyncConfig(client, gameConfig, currentConfig);
//                client.Disconnect();
//            }

//            vm.Game = gameConfig;
//            vm.CurrentConfig = currentConfig;
//            vm.Infos = _service.GetGameInfos(gameServer, _service.GetRunningProcesses(gameConfig.Server));
//            vm.ConfigFiles = gameConfig.ConfigFiles.Select((f, i) => new ConfigFileInfos()
//            {
//                Index = i,
//                Name = Path.GetFileName(f)
//            }).ToList();

//            return View(vm);
//        }


//        private string GetLog(GameConfig game, SftpClient client)
//        {
//            if (game.ConsoleFile != null && client.Exists(game.ConsoleFile))
//            {
//                return client.ReadAllText(game.ConsoleFile);
//            }
//            if (game.ConsoleFileDirectory != null && game.ConsoleFilePrefix != null)
//            {
//                var files = client.ListDirectory(game.ConsoleFileDirectory);
//                var file = files.Where(f => f.Name.StartsWith(game.ConsoleFilePrefix, StringComparison.OrdinalIgnoreCase)).OrderByDescending(f => f.LastWriteTimeUtc).FirstOrDefault();
//                if (file != null)
//                {
//                    return client.ReadAllText(file.FullName);
//                }
//            }
//            return string.Empty;
//        }

//        private ModInfoVM GetModInfos(SftpClient client, string directory, string dirname)
//        {
//            var safeId = dirname.Replace("@", "");
//            return _cache.GetOrCreate("Arma3Mod@" + safeId, _ => GetModInfosDirect(client, directory, dirname, safeId));
//        }

//        private static ModInfoVM GetModInfosDirect(SftpClient client, string directory, string dirname, string safeId)
//        {
//            var metaFile = directory + "/" + dirname + "/meta.cpp";
//            if (client.Exists(metaFile))
//            {
//                var meta = client.ReadAllText(metaFile);
//                var matchName = NameRegex.Match(meta);
//                var id = safeId;
//                if (!IsIdRegex.IsMatch(safeId))
//                {
//                    var matchId = IdRegex.Match(meta);
//                    id = matchId.Success ? matchId.Groups[1].Value : safeId;
//                }
//                var name = matchName.Success ? matchName.Groups[1].Value : safeId;
//                return new ModInfoVM() { Id = id, Name = name, Href = "http://steamcommunity.com/sharedfiles/filedetails/?id=" + id };
//            }
//            return new ModInfoVM() { Id = safeId, Name = safeId, Href = "http://steamcommunity.com/sharedfiles/filedetails/?id=" + safeId };
//        }

//        [HttpGet]
//        public IActionResult FullLog(string id)
//        {
//            var game = _service.Games.FirstOrDefault(g => g.Name == id);
//            if (game == null)
//            {
//                return BadRequest();
//            }
//            var vm = new ServerInfosViewModel();
//            vm.Infos = _service.GetGameInfos(game, _service.GetRunningProcesses(game.Server));
//            using (var client = _service.GetSftpClient(game.Server))
//            {
//                client.Connect();
//                vm.Console = GetLog(game, client);
//                client.Disconnect();
//            }
//            const int ConsoleMaxLength = 5_000_000;
//            if (vm.Console != null && vm.Console.Length > ConsoleMaxLength)
//            {
//                vm.Console = vm.Console.Substring(vm.Console.Length - ConsoleMaxLength, ConsoleMaxLength);
//            }
//            return View(vm);
//        }

//        public IActionResult DownloadMission(string id, string mission)
//        {
//            var game = _service.Games.FirstOrDefault(g => g.Name == id);
//            if (game == null)
//            {
//                return BadRequest();
//            }

//            var name = Path.GetFileName(mission);
//            var sourceFile = game.MissionDirectory + "/" + name;

//            var mem = new MemoryStream();
//            using (var client = _service.GetSftpClient(game.Server))
//            {
//                client.Connect();
//                client.DownloadFile(sourceFile, mem);
//                client.Disconnect();
//            }

//            return new FileContentResult(mem.ToArray(), "application/octet-stream")
//            {
//                FileDownloadName = name
//            };
//        }

//        [ValidateAntiForgeryToken]
//        [HttpPost]
//        public IActionResult UploadMission(string id, IFormFile file)
//        {
//            var game = _service.Games.FirstOrDefault(g => g.Name == id);
//            if (game == null)
//            {
//                return BadRequest();
//            }

//            var name = Path.GetFileName(file.FileName);

//            var targetFile = game.MissionDirectory + "/" + name;

//            using (var client = _service.GetSftpClient(game.Server))
//            {
//                client.Connect();

//                BackupIfExists(targetFile, client);

//                using (var source = file.OpenReadStream())
//                {
//                    client.UploadFile(source, targetFile, true);
//                }
//                client.Disconnect();
//            }
//            return RedirectToAction(nameof(ServerInfosAsync), new { id });
//        }

//        private static void BackupIfExists(string targetFile, SftpClient client)
//        {
//            if (client.Exists(targetFile))
//            {
//                var backupName = targetFile + ".old";
//                var i = 2;
//                while (client.Exists(backupName))
//                {
//                    backupName = targetFile + ".old" + i;
//                    i++;
//                }
//                client.RenameFile(targetFile, backupName);
//            }
//        }

//    }
//}