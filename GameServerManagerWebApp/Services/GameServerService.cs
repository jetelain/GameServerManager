using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using GameServerManagerWebApp.Entites;
using GameServerManagerWebApp.Models;
using GameServerManagerWebApp.Services.Arma3Mods;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace GameServerManagerWebApp.Services
{
    public class GameServerService
    {
        private readonly ILogger<GameServerService> _logger;
        private readonly IConfiguration _config;
        private readonly GameServerManagerContext _context;
        private readonly IHttpClientFactory _factory;
        private readonly ISshService _sshService;

        private readonly static object locker = new object();
        private readonly static List<GameServerState> gameServersState = new List<GameServerState>();

        internal static readonly Regex PasswordRegex = new Regex(@"password\s*=\s*""(.*)""", RegexOptions.IgnoreCase);
        internal static readonly Regex LabelRegex = new Regex(@"hostname\s*=\s*""(.*)""", RegexOptions.IgnoreCase);
        internal static readonly Regex MissionRegex = new Regex(@"class\s+Missions\s*{[^}]*class\s+[a-zA-Z0-9]+\s*{\s*template\s*=\s*([^;]*);", RegexOptions.IgnoreCase| RegexOptions.Multiline);



        public GameServerService(ILogger<GameServerService> logger, IConfiguration config, GameServerManagerContext context, IHttpClientFactory factory, ISshService sshService)
        {
            _logger = logger;
            _config = config;
            _context = context;
            _factory = factory;
            _sshService = sshService;
        }

        internal async Task SyncAll()
        {
            foreach (var server in await _context.HostServers.ToListAsync())
            {
                await _sshService.RunSftpAsync(server, async client =>
                {
                    foreach (var currentConfig in await _context.GameServerConfigurations
                        .Include(c => c.GameServer)
                        .Include(c => c.Files)
                        .Include(c => c.Modset)
                        .Where(s => s.GameServer.HostServerID == server.HostServerID && s.IsActive)
                        .ToListAsync())
                    {
                        await SyncConfig(client, currentConfig);
                    }
                });
            }
        }

        //internal SftpClient GetSftpClient(HostServer server)
        //{
        //    return new SftpClient(server.Address, server.SshUserName, GetPassword(server));
        //}


        internal GameServerInfo GetGameInfos(GameServer g, List<ProcessInfo> processes)
        {
            var game = GetConfig(g);
            var state = GetOrCreateState(g);
            var gameProcesses = processes.Where(p => (p.User == game.TopUserName || game.OtherUsers.Contains(p.User)) && game.Command.Contains(p.Cmd) && game.Server == p.Server).ToList();

            return new GameServerInfo()
            {
                GameServer = g,
                Name = game.Name,
                IsRunning = gameProcesses.Any(p => p.User == game.TopUserName),
                Cpu = gameProcesses.Sum(p => p.Cpu),
                Mem = gameProcesses.Sum(p => p.Mem),
                Processes = gameProcesses,
                IsUpdating = state.IsUpdating,
                LastUpdateResult = state.IsUpdating ? null : state.GetLastUpdateResult()
            };
        }

        internal GameConfig GetConfig(GameServer server)
        {
            if (server.HostServerID == null)
            {
                throw new ArgumentException();
            }
            if (server.Type == GameServerType.Arma3)
            {
                return new GameConfig()
                {
                    Server = server.HostServer,
                    Name = server.UserName + "@" + server.HostServer.Name,
                    TopUserName = TopTruncate(server.UserName),
                    StartCmd = "sudo -H -u " + server.UserName + " /home/" + server.UserName + "/start.sh",
                    StopCmd = "sudo -H -u " + server.UserName + " /home/" + server.UserName + "/stop.sh",
                    // UpdateCmd = "sudo -H -u " + server.UserName + " /home/" + server.UserName + "/updateserver.sh", ==> Disabled as we need to login to steamcmd, may prompt for password and steam guard code
                    ConfigFiles = new[]{
                        "server.cfg",
                        "basic.cfg",
                        "mods.txt",
                        "servermods.txt"
                    },
                    MissionDirectory = server.BasePath.TrimEnd('/') + "/mpmissions",
                    GameBaseDirectory = server.BasePath.TrimEnd('/'),
                    ConsoleFileDirectory = server.BasePath.TrimEnd('/') + "/profiles",
                    ConsoleFilePrefix = "arma3server_",
                    Command = new[] { "arma3server", "arma3server_x64", "MainThrd" },
                    OtherUsers = (server.UserName == "arma3-w" || server.UserName == "arma3") ? new[] { "hc1", "hc2" } : new[] { "hc3", "hc4", "hc5", "hc6" }, // TODO: trouver un moyen de gérer ça
                };
            }

            if (server.Type == GameServerType.Squad)
            {
                return new GameConfig()
                {
                    Server = server.HostServer,
                    Name = server.UserName + "@" + server.HostServer.Name,
                    TopUserName = TopTruncate(server.UserName),
                    StartCmd = "sudo -H -u " + server.UserName + " /home/" + server.UserName + "/start.sh",
                    StopCmd = "sudo -H -u " + server.UserName + " /home/" + server.UserName + "/stop.sh",
                    UpdateCmd = "sudo -H -u " + server.UserName + " /home/" + server.UserName + "/updateserver.sh",
                    ConfigFiles = new[]{
                        "SquadGame/ServerConfig/Admins.cfg",
                        "SquadGame/ServerConfig/Bans.cfg",
                        "SquadGame/ServerConfig/CustomOptions.cfg",
                        "SquadGame/ServerConfig/ExcludedFactions.cfg",
                        "SquadGame/ServerConfig/ExcludedFactionSetups.cfg",
                        "SquadGame/ServerConfig/ExcludedLayers.cfg",
                        "SquadGame/ServerConfig/ExcludedLevels.cfg",
                        "SquadGame/ServerConfig/LayerRotation.cfg",
                        "SquadGame/ServerConfig/LevelRotation.cfg",
                        "SquadGame/ServerConfig/MOTD.cfg",
                        "SquadGame/ServerConfig/Rcon.cfg",
                        "SquadGame/ServerConfig/Server.cfg",
                        "SquadGame/ServerConfig/ServerMessages.cfg",
                        "SquadGame/ServerConfig/VoteConfig.cfg"
                    },
                    // MissionDirectory = server.BasePath.TrimEnd('/') + "/mpmissions",
                    GameBaseDirectory = server.BasePath.TrimEnd('/'),
                    ConsoleFileDirectory = server.BasePath.TrimEnd('/') + "/SquadGame/Saved/Logs",
                    ConsoleFilePrefix = "",
                    Command = new[] { "SquadGameServer",},
                    OtherUsers = new string[0]
                };
            }
            if (server.Type == GameServerType.ArmaReforger)
            {
                return new GameConfig()
                {
                    Server = server.HostServer,
                    Name = server.UserName + "@" + server.HostServer.Name,
                    TopUserName = TopTruncate(server.UserName),
                    StartCmd = "sudo -H -u " + server.UserName + " /home/" + server.UserName + "/start.sh",
                    StopCmd = "sudo -H -u " + server.UserName + " /home/" + server.UserName + "/stop.sh",
                    UpdateCmd = "sudo -H -u " + server.UserName + " /home/" + server.UserName + "/updateserver.sh",
                    ConfigFiles = new[] {
                        "config.json"
                    },
                    GameBaseDirectory = server.BasePath.TrimEnd('/'),
                    ConsoleDirectoryBase = server.BasePath.TrimEnd('/') + "/user/logs",
                    ConsoleDirectoryFile = "console.log",
                    Command = new[] { "enfMain" },
                    OtherUsers = new string[0]
                };
            }

            
            throw new NotImplementedException($"Unsupported server type {server.Type} for {server.GameServerID}");
        }

        private string TopTruncate(string userName)
        {
            if (userName.Length > 8 )
            {
                return userName.Substring(0, 7) + "+";
            }
            return userName;
        }

        internal async Task<List<ProcessInfo>> GetRunningProcesses()
        {
            List<ProcessInfo> processes = new List<ProcessInfo>();
            foreach (var server in _context.HostServers)
            {
                processes.AddRange(await GetRunningProcesses(server));
            }
            return processes;
        }

        internal async Task<bool> StartGameServer(ClaimsPrincipal user, GameServerConfiguration currentConfig)
        {
            var state = GetOrCreateState(currentConfig.GameServer);

            if (state.IsUpdating)
            {
                return false;
            }

            await LogServerEvent(user, currentConfig.GameServer, GameLogEventType.ServerStart);

            var game = GetConfig(currentConfig.GameServer);

            await ApplyAllConfiguration(currentConfig, game);

            await _sshService.RunCommandAsync(game.Server, game.StartCmd);

            await Task.Delay(100);

            return true;
        }

        internal async Task ApplyAllConfiguration(GameServerConfiguration currentConfig)
        {
            await ApplyAllConfiguration(currentConfig, GetConfig(currentConfig.GameServer));
        }

        private async Task ApplyAllConfiguration(GameServerConfiguration currentConfig, GameConfig game)
        {
            if (!currentConfig.IsActive || currentConfig.GameServer.SyncFiles.Count > 0)
            {
                await _sshService.RunSftpAsync(currentConfig.GameServer.HostServer, async client =>
                {
                    if (!currentConfig.IsActive)
                    {
                        await ApplyConfiguration(currentConfig, game, client);
                    }
                    if (currentConfig.GameServer.SyncFiles.Count > 0)
                    {
                        await UpdateSyncedFiles(currentConfig.GameServer, game, client);
                    }
                });
            }
        }

        private async Task LogServerEvent(ClaimsPrincipal user, GameServer gameServer, GameLogEventType type)
        {
            _context.GameLogEvents.Add(new GameLogEvent()
            {
                GameServerID = gameServer.GameServerID,
                SteamId = SteamHelper.GetSteamId(user),
                Timestamp = DateTime.UtcNow,
                Type = type
            });
            await _context.SaveChangesAsync();
        }

        private async Task ApplyConfiguration(GameServerConfiguration currentConfig, GameConfig game, SftpClient client)
        {
            foreach (var configFile in currentConfig.Files)
            {
                var fullpath = GetFileFullPath(game, configFile.Path);
                if (!client.Exists(fullpath) || client.ReadAllText(fullpath) != configFile.Content)
                {
                    WriteAllText(client, fullpath, configFile.Content);
                }
            }
            var activeList = await _context.GameServerConfigurations.Where(c => c.GameServerID == currentConfig.GameServerID && c.IsActive).ToListAsync();
            foreach(var active in activeList)
            {
                active.IsActive = false;
                _context.Update(active);
            }
            currentConfig.IsActive = true;
            _context.Update(currentConfig);
            await _context.SaveChangesAsync();
        }

        internal async Task UpdateSyncedFiles(GameServer server)
        {
            var game = GetConfig(server);

            await _sshService.RunSftpAsync(server.HostServer, async client =>
            {
                await UpdateSyncedFiles(server, game, client);
            });
        }

        private async Task UpdateSyncedFiles(GameServer server, GameConfig game, SftpClient client)
        {
            foreach (var syncFile in server.SyncFiles)
            {
                var content = await GetSyncContent(syncFile);
                if (syncFile.Content != content)
                {
                    syncFile.Content = content;
                    syncFile.LastChangeUTC = DateTime.UtcNow;
                    _context.Update(syncFile);
                    await _context.SaveChangesAsync();
                }
                var fullpath = GetFileFullPath(game, syncFile.Path);
                if (!client.Exists(fullpath) || client.ReadAllText(fullpath) != syncFile.Content)
                {
                    WriteAllText(client, fullpath, syncFile.Content);
                }
            }
        }

        private async Task<string> GetSyncContent(GameServerSyncedFile syncFile)
        {
            try
            {
                return await _factory.CreateClient().GetStringAsync(syncFile.SyncUri);
            }
            catch(Exception e)
            {
                _logger.LogWarning(e, "Unable to read '{0}'", syncFile.SyncUri);
                return syncFile.Content;
            }
        }

        internal async Task StopGameServer(ClaimsPrincipal user, GameServer gameServer)
        {
            await LogServerEvent(user, gameServer, GameLogEventType.ServerStop);

            var game = GetConfig(gameServer);

            await _sshService.RunCommandAsync(game.Server, game.StopCmd);

            await Task.Delay(100);
        }

        private static readonly Regex Space = new Regex("\\s+", RegexOptions.Compiled);

        public async Task<List<ProcessInfo>> GetRunningProcesses(HostServer server)
        {
            try
            {
                var top = await _sshService.RunCommandAsync(server, "/usr/bin/top -n 1 -b -w 200");

                return top.Result.Split("\n").Skip(7).Select(l => Space.Split(l.Trim())).Where(items => items.Length == 12).Select(items => new ProcessInfo()
                {
                    Pid = int.Parse(items[0], CultureInfo.InvariantCulture),
                    User = items[1],
                    Cpu = decimal.Parse(items[8], CultureInfo.InvariantCulture),
                    Mem = decimal.Parse(items[9], CultureInfo.InvariantCulture),
                    Cmd = items[11],
                    Server = server
                }).ToList();
            }
            catch (SshOperationTimeoutException)
            {
                return new List<ProcessInfo>();
            }
        }

        internal async Task SyncConfig(SftpClient client, GameServerConfiguration currentConfig)
        {
            await SyncConfig(client, GetConfig(currentConfig.GameServer), currentConfig);
        }

        internal async Task SyncConfig(SftpClient client, GameConfig config, GameServerConfiguration currentConfig)
        {
            var wasUpdated = false;
            foreach (var file in config.ConfigFiles)
            {
                string fullPath = GetFileFullPath(config, file);
                var content = string.Empty;
                var lastWriteUTC = DateTime.MinValue;
                if (client.Exists(fullPath))
                {
                    content = client.ReadAllText(fullPath);
                    lastWriteUTC = client.GetLastWriteTimeUtc(fullPath);
                }
                var dbFile = currentConfig.Files.FirstOrDefault(f => f.Path == file);
                if (dbFile == null)
                {
                    dbFile = new GameConfigurationFile()
                    {
                        Configuration = currentConfig,
                        Content = content,
                        LastChangeUTC = lastWriteUTC,
                        Path = file
                    };
                    currentConfig.Files.Add(dbFile);
                    _context.Add(dbFile);
                    wasUpdated = true;
                }
                else if (dbFile.Content != content)
                {
                    dbFile.Content = content;
                    dbFile.LastChangeUTC = lastWriteUTC;
                    _context.Update(dbFile);
                    wasUpdated = true;
                }
            }

            if (wasUpdated)
            {
                await UpdateComputedProperties(currentConfig);
                _context.GameServerConfigurations.Update(currentConfig);
                await _context.SaveChangesAsync();
            }
            else if (currentConfig.Modset == null)
            {
                if (currentConfig.GameServer.Type == GameServerType.Arma3)
                {
                    await UpdateModset(currentConfig);
                    if (currentConfig.Modset != null)
                    {
                        _context.GameServerConfigurations.Update(currentConfig);
                        await _context.SaveChangesAsync();
                    }
                }
            }
        }

        internal string GetFileFullPath(GameConfig config, string file)
        {
            return config.GameBaseDirectory.TrimEnd('/') + '/' + file;
        }

        internal async Task UpdateComputedProperties(GameServerConfiguration currentConfig)
        {
            if (currentConfig.GameServer.Type == GameServerType.Arma3)
            {
                var cfg = currentConfig.Files.First(f => f.Path == "server.cfg").Content;

                var matchPassword = PasswordRegex.Match(cfg);
                currentConfig.ServerPassword = matchPassword.Success ? matchPassword.Groups[1].Value : "";

                var matchLabel = LabelRegex.Match(cfg);
                currentConfig.ServerName = matchLabel.Success ? matchLabel.Groups[1].Value : currentConfig.GameServer.Label;

                var matchMission = MissionRegex.Match(cfg);
                currentConfig.ServerMission = matchMission.Success ? matchMission.Groups[1].Value : "";

                await UpdateModset(currentConfig);

                currentConfig.LastChangeUTC = currentConfig.Files.Max(f => f.LastChangeUTC);
            }
        }

        private async Task UpdateModset(GameServerConfiguration currentConfig)
        {
            var mods = currentConfig.Files.First(f => f.Path == "mods.txt").Content;
            if (currentConfig.Modset == null || currentConfig.Modset.ConfigurationFile != mods)
            {
                currentConfig.Modset = await _context.Modsets.Where(m => m.ConfigurationFile == mods).FirstOrDefaultAsync();
                currentConfig.ModsetID = currentConfig.Modset?.ModsetID;
            }
        }

        internal void WriteAllText(SftpClient client, string path, string content)
        {
            client.UploadFile(new MemoryStream(Encoding.UTF8.GetBytes(content)), path, true);
        }

        internal static string GenerateToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }

        internal async Task<string> ValidateModsetOnServer(Modset actualModset, GameServer server)
        {
            var config = GetConfig(server);
            return await _sshService.RunSftpAsync<string>(config.Server, async client =>
            {
                var analyze = AnalyseModsetFile(config, XDocument.Parse(actualModset.DefinitionFile), client);
                if (analyze.Any(m => !m.IsOK))
                {
                    return ToErrorMessage(analyze);
                }
                return null;
            });
        }

        private string ToErrorMessage(List<SetupArma3Mod> analyze)
        {
            return string.Join("\r\n ", analyze.Where(e => !e.IsOK).Select(e => e.Message));
        }

        private List<SetupArma3Mod> AnalyseModsetFile(GameConfig game, XDocument doc, SftpClient client)
        {
            var defintions = ModsetFileHelper.GetModsetEntries(doc);
            var mods = new List<SetupArma3Mod>();
            foreach (var mod in defintions)
            {
                if (!string.IsNullOrEmpty(mod.SteamId))
                {
                    if (client == null || client.Exists(game.GameBaseDirectory + "/@" + mod.SteamId))
                    {
                        mods.Add(new SetupArma3Mod() { Id = mod.SteamId, Name = mod.Name, Href = mod.Href, IsOK = true });
                    }
                    else
                    {
                        mods.Add(new SetupArma3Mod() { Id = mod.SteamId, Name = mod.Name, Href = mod.Href, IsOK = false, Message = $"Mod '{mod.Name}' non installé sur le serveur ({mod.SteamId})." });
                    }
                }
                else
                {
                    mods.Add(new SetupArma3Mod() { Name = mod.Name, Href = mod.Href, IsOK = false, Message = $"Mod '{mod.Name}' non disponible sur le Workshop." });
                }
            }
            return mods;
        }

        internal async Task<string> ParseArma3Modset(Modset modset, IFormFile data)
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
                return ToErrorMessage(analyze);
            }
            modset.ConfigurationFile = string.Join(";", analyze.Select(m => "@" + m.Id));
            return null;
        }

        internal async Task<string> UploadMissionFile(IFormFile file, GameServer gameServer)
        {
            if (gameServer.Type != GameServerType.Arma3)
            {
                throw new ArgumentException();
            }
            var config = GetConfig(gameServer);
            var name = Path.GetFileName(file.FileName);
            var targetFile = config.MissionDirectory + "/" + name;
            await _sshService.RunSftpAsync(config.Server, async client =>
            {
                BackupIfExists(targetFile, client);
                using (var source = file.OpenReadStream())
                {
                    client.UploadFile(source, targetFile, true);
                }
            });
            return Path.GetFileNameWithoutExtension(name);
        }

        private static void BackupIfExists(string targetFile, SftpClient client)
        {
            if (client.Exists(targetFile))
            {
                var backupName = targetFile + ".old";
                var i = 2;
                while (client.Exists(backupName))
                {
                    backupName = targetFile + ".old" + i;
                    i++;
                }
                client.RenameFile(targetFile, backupName);
            }
        }

        internal async Task AskUpdateGameServer(ClaimsPrincipal user, GameServer gameServer)
        {
            await LogServerEvent(user, gameServer, GameLogEventType.UpdateServer);

            var game = GetConfig(gameServer);

            var state = GetOrCreateState(gameServer);

            if (state.IsUpdating)
            {
                return;
            }

            state.UpdateTask = Task.Run(async () => await DoUpdateGameServer(state, game));
        }

        private async Task<InstallResult> DoUpdateGameServer(GameServerState state, GameConfig game)
        {
            await state.UpdateSemaphore.WaitAsync();
            try
            {
                var started = DateTime.UtcNow;

                await _sshService.RunCommandAsync(game.Server, game.StopCmd);

                await Task.Delay(2500);

                var result = await _sshService.RunLongCommandAsync(game.Server, game.UpdateCmd);

                var finished = DateTime.UtcNow;

                return new InstallResult(started, finished, result.ExitStatus, result.Error + result.Result);
            }
            finally
            {
                state.UpdateSemaphore.Release();
            }
        }

        private GameServerState GetOrCreateState(GameServer server)
        {
            lock (locker)
            {
                var state = gameServersState.FirstOrDefault(s => s.GameServerID == server.GameServerID);
                if (state == null)
                {
                    state = new GameServerState()
                    {
                        GameServerID = server.GameServerID
                    };
                    gameServersState.Add(state);
                }
                return state;
            }
        }

        internal void ClearUpdateResult(GameServer gameServer)
        {
            var state = GetOrCreateState(gameServer);
            if (!state.IsUpdating)
            {
                state.UpdateTask = null;
            }
        }
    }
}
