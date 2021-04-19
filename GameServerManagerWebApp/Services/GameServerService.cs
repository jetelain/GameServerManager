using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GameServerManagerWebApp.Entites;
using GameServerManagerWebApp.Models;
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

        internal static readonly Regex PasswordRegex = new Regex(@"password\s*=\s*""(.*)""", RegexOptions.IgnoreCase);
        internal static readonly Regex LabelRegex = new Regex(@"hostname\s*=\s*""(.*)""", RegexOptions.IgnoreCase);
        internal static readonly Regex MissionRegex = new Regex(@"class\s+Missions\s*{[^}]*class\s+[a-zA-Z0-9]+\s*{\s*template\s*=\s*([^;]*);", RegexOptions.IgnoreCase| RegexOptions.Multiline);

        public GameServerService(ILogger<GameServerService> logger, IConfiguration config, GameServerManagerContext context, IHttpClientFactory factory)
        {
            _logger = logger;
            _config = config;
            _context = context;
            _factory = factory;
        }

        internal SshClient GetClient(HostServer server)
        {
            return new SshClient(server.Address, server.SshUserName, GetPassword(server));
        }

        private string GetPassword(HostServer server)
        {
            var servers = _config.GetSection("Servers");
            var entry = servers.GetSection(server.Address);
            if (entry == null)
            {
                throw new ArgumentOutOfRangeException();
            }
            return entry.Value;
        }

        internal async Task SyncAll()
        {
            foreach (var server in await _context.HostServers.ToListAsync())
            {
                using (var client = GetSftpClient(server))
                {
                    client.Connect();
                    foreach (var currentConfig in await _context.GameServerConfigurations
                        .Include(c => c.GameServer)
                        .Include(c => c.Files)
                        .Include(c => c.Modset)
                        .Where(s => s.GameServer.HostServerID == server.HostServerID && s.IsActive)
                        .ToListAsync())
                    {
                        await SyncConfig(client, currentConfig);
                    }
                    client.Disconnect();
                }
            }
        }

        internal SftpClient GetSftpClient(HostServer server)
        {
            return new SftpClient(server.Address, server.SshUserName, GetPassword(server));
        }


        internal GameServerInfo GetGameInfos(GameServer g, List<ProcessInfo> processes)
        {
            var game = GetConfig(g);
            var gameProcesses = processes.Where(p => (p.User == game.TopUserName || game.OtherUsers.Contains(p.User)) && game.Command.Contains(p.Cmd) && game.Server == p.Server).ToList();

            return new GameServerInfo()
            {
                GameServer = g,
                Name = game.Name,
                Running = gameProcesses.Any(p => p.User == game.TopUserName),
                Cpu = gameProcesses.Sum(p => p.Cpu),
                Mem = gameProcesses.Sum(p => p.Mem),
                Processes = gameProcesses
            };
        }

        internal GameConfig GetConfig(GameServer server)
        {
            if (server.Type == GameServerType.Arma3)
            {
                return new GameConfig()
                {
                    Server = server.HostServer,
                    Name = server.UserName + "@" + server.HostServer.Name,
                    TopUserName = TopTruncate(server.UserName),
                    StartCmd = "sudo -H -u " + server.UserName + " /home/" + server.UserName + "/start.sh",
                    StopCmd = "sudo -H -u " + server.UserName + " /home/" + server.UserName + "/stop.sh",
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
                    Command = new[] { "arma3server", "arma3server_x64" },
                    OtherUsers = server.UserName == "arma3-w" ? new[] { "hc1", "hc2" } : new[] { "hc5", "hc6" }, // TODO: trouver un moyen de gérer ça
                };
            }
            throw new NotImplementedException();
        }

        private string TopTruncate(string userName)
        {
            if (userName.Length > 8 )
            {
                return userName.Substring(0, 7) + "+";
            }
            return userName;
        }

        internal List<ProcessInfo> GetRunningProcesses(HostServer server)
        {
            List<ProcessInfo> processes;
            try
            {
                using (var client = GetClient(server))
                {
                    client.ConnectionInfo.Timeout = TimeSpan.FromMilliseconds(1000);
                    client.Connect();
                    processes = GetRunningProcesses(client, server);
                    client.Disconnect();
                }
            }
            catch (SshOperationTimeoutException)
            {
                processes = new List<ProcessInfo>();
            }
            return processes;
        }

        internal List<ProcessInfo> GetRunningProcesses()
        {
            List<ProcessInfo> processes = new List<ProcessInfo>();
            foreach (var server in _context.HostServers)
            {
                processes.AddRange(GetRunningProcesses(server));
            }
            return processes;
        }

        internal async Task StartGameServer(ClaimsPrincipal user, GameServerConfiguration currentConfig)
        {
            await LogServerEvent(user, currentConfig.GameServer, GameLogEventType.ServerStart);

            var game = GetConfig(currentConfig.GameServer);

            if (!currentConfig.IsActive || currentConfig.GameServer.SyncFiles.Count > 0)
            {
                using (var client = GetSftpClient(currentConfig.GameServer.HostServer))
                {
                    client.Connect();
                    if (!currentConfig.IsActive)
                    {
                        await ApplyConfiguration(currentConfig, game, client);
                    }
                    if (currentConfig.GameServer.SyncFiles.Count > 0)
                    {
                        await UpdateSyncedFiles(currentConfig, game, client);
                    }
                    client.Disconnect();
                }
            }

            using (var client = GetClient(game.Server))
            {
                client.Connect();
                var result = client.RunCommand(game.StartCmd);
                Thread.Sleep(100);
                client.Disconnect();
            }
        }

        private async Task LogServerEvent(ClaimsPrincipal user, GameServer gameServer, GameLogEventType type)
        {
            _context.GameLogEvents.Add(new GameLogEvent()
            {
                GameServerID = gameServer.GameServerID,
                SteamId = SteamHelper.GetSteamId(user),
                Timestamp = DateTime.Now,
                Type = type
            });
            await _context.SaveChangesAsync();
        }

        private async Task ApplyConfiguration(GameServerConfiguration currentConfig, GameConfig game, SftpClient client)
        {
            foreach (var configFile in currentConfig.Files)
            {
                var fullpath = GetFileFullPath(game, configFile.Path, true);
                if (!client.Exists(fullpath) || client.ReadAllText(fullpath) != configFile.Content)
                {
                    client.WriteAllText(fullpath, configFile.Content);
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

        private async Task UpdateSyncedFiles(GameServerConfiguration currentConfig, GameConfig game, SftpClient client)
        {
            foreach (var syncFile in currentConfig.GameServer.SyncFiles)
            {
                var content = await GetSyncContent(syncFile);
                if (syncFile.Content != content)
                {
                    syncFile.Content = content;
                    syncFile.LastChangeUTC = DateTime.UtcNow;
                    _context.Update(syncFile);
                    await _context.SaveChangesAsync();
                }
                var fullpath = GetFileFullPath(game, syncFile.Path, true);
                if (!client.Exists(fullpath) || client.ReadAllText(fullpath) != syncFile.Content)
                {
                    client.WriteAllText(fullpath, syncFile.Content);
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

            using (var client = GetClient(game.Server))
            {
                client.Connect();
                var result = client.RunCommand(game.StopCmd);
                Thread.Sleep(100);
                client.Disconnect();
            }
        }

        private static readonly Regex Space = new Regex("\\s+", RegexOptions.Compiled);

        private static List<ProcessInfo> GetRunningProcesses(SshClient client, HostServer server)
        {
            var top = client.RunCommand("/usr/bin/top -n 1 -b -w 200");
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

        internal async Task SyncConfig(SftpClient client, GameServerConfiguration currentConfig)
        {
            await SyncConfig(client, GetConfig(currentConfig.GameServer), currentConfig);
        }

        internal async Task SyncConfig(SftpClient client, GameConfig config, GameServerConfiguration currentConfig)
        {
            var wasUpdated = false;
            foreach (var file in config.ConfigFiles)
            {
                string fullPath = GetFileFullPath(config, file, false);
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
                await UpdateModset(currentConfig);
                if (currentConfig.Modset != null)
                {
                    _context.GameServerConfigurations.Update(currentConfig);
                    await _context.SaveChangesAsync();
                }
            }
        }

        internal string GetFileFullPath(GameConfig config, string file, bool forWrite)
        {
            var fullpath = config.GameBaseDirectory.TrimEnd('/') + '/' + file;
            if (forWrite && _config.GetValue<bool>("DoNotWriteRealFiles", false))
            {
                fullpath = fullpath + ".test";
            }
            return fullpath;
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

        internal static string GenerateToken()
        {
            var random = new byte[32];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(random);
            }
            return Convert.ToBase64String(random).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }
    }
}
