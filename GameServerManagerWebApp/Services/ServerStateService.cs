using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Arma3ServerToolbox.ArmaPersist;
using GameServerManagerWebApp.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using Renci.SshNet.Sftp;

namespace GameServerManagerWebApp.Services
{
    public class ServerStateService
    {
        private static readonly Regex ArmaLineRegex = new Regex("([0-9]{4})/([ 0-9]{1,2})/([ 0-9]{1,2}), ([ 0-9]{2}):([ 0-9]{2}):([ 0-9]{2}) (.*)", RegexOptions.Compiled);
        private static readonly Regex ArmaConnectRegex = new Regex(@"\[GTD\] \(persistence\) INFO: (Restore Player|Saved) .*uid=([0-9]+) ", RegexOptions.Compiled);
        private static readonly Regex AramaSteamIdQuotedRegex = new Regex(@"""([0-9]{17})""", RegexOptions.Compiled);

        private readonly ILogger<ServerStateService> _logger;
        private readonly GameServerManagerContext _context;
        private readonly GameServerService _service;
        private static int pollingLock = 0;
        private static int snapshotLock = 0;

        public ServerStateService(ILogger<ServerStateService> logger, GameServerManagerContext context, GameServerService service)
        {
            _logger = logger;
            _context = context;
            _service = service;
        }

        public async Task<List<GameLogEvent>> GetConnectedPlayersRealTime(bool forcePolling = false)
        {
            if (forcePolling)
            {
                await Poll();
            }
            return await _context.GameLogEvents.Include(e => e.Server).Where(p => p.Type == GameLogEventType.Connect && !p.IsFinished).ToListAsync();
        }

        public async Task<List<GameLogEvent>> GetConnectedPlayersInThePast(DateTime dt)
        {
            var min = dt.AddDays(-2);
            return (await _context.GameLogEvents.Include(e => e.Server)
                .Where(p => p.Type == GameLogEventType.Connect && p.Timestamp <= dt && p.Timestamp > min)
                .ToListAsync())
                .Where(p => !p.IsFinished || dt < p.Timestamp + p.Duration)
                .ToList();
        }
        
        public async Task TakePersistSnapshots()
        {
            var sw = Stopwatch.StartNew();
            var isSnapshotInProgress = Interlocked.Increment(ref snapshotLock) > 1;
            try
            {
                if (!isSnapshotInProgress)
                {
                    foreach (var server in await _context.GameServers.Where(g => g.Type == GameServerType.Arma3).Include(s => s.HostServer).ToListAsync())
                    {
                        await TakePersistSnapshots(server, _service.GetConfig(server));
                    }
                }
            }
            finally
            {
                Interlocked.Decrement(ref snapshotLock);
            }
            _logger.LogInformation("Snapshot took {0} msec, isSnapshotInProgress: {1}", sw.ElapsedMilliseconds, isSnapshotInProgress);

        }
        private async Task TakePersistSnapshots(GameServer server, GameConfig config)
        {
            var latsInvTimestamp = await _context.GamePersistSnapshots.Where(i => i.GameServerID == server.GameServerID).MaxAsync(i => i.Timestamp);

            foreach(var backup in DownloadBackup(config, latsInvTimestamp))
            { 
                var inv = new GamePersistSnapshot() 
                { 
                    Timestamp = backup.LastChange, 
                    GameServer = server, 
                    Backup = JsonSerializer.Serialize(backup),
                    GamePersistName = backup.Name
                };
                _context.GamePersistSnapshots.Add(inv);
                await _context.SaveChangesAsync();
                /*
                var countByItem = backup.Boxes.SelectMany(b => b.Items).GroupBy(i => i.Name).Select(i => new PersistItem(i.Key, i.Sum(j => j.Count))).ToList();
                foreach (var entry in countByItem)
                {
                    _context.InventoryItems.Add(new GameInventoryItem() { Snapshot = inv, Item = await GetItem(entry.Name), Count = (int)entry.Count });
                }
                await _context.SaveChangesAsync();*/
            }
        }
        /*
        private async Task<GameItem> GetItem(string name)
        {
            var item = await _context.Items.FirstOrDefaultAsync(i => i.Name == name);
            if ( item == null)
            {
                item = new GameItem()
                {
                    Name = name,
                    Label = name,
                    GroupWeight = 1
                };
                if (name.Contains("100Rnd"))
                {
                    item.GroupWeight = 100;
                }
                else if (name.Contains("20Rnd"))
                {
                    item.GroupWeight = 20;
                }
                else if (name.Contains("30Rnd"))
                {
                    item.GroupWeight = 30;
                }
                else if (name.Contains("17Rnd"))
                {
                    item.GroupWeight = 17;
                }
                else if (name.Contains("16Rnd"))
                {
                    item.GroupWeight = 16;
                }
                else if (name.Contains("10Rnd"))
                {
                    item.GroupWeight = 10;
                }
                _context.Items.Add(item);
                await _context.SaveChangesAsync();
            }
            return item;
        }
        */
        private List<PersistBackup> DownloadBackup(GameConfig config, DateTime last)
        {
            List<PersistBackup> backup = new List<PersistBackup>();
            using (var client = _service.GetSftpClient(config.Server))
            {
                client.Connect();
                var file = config.ConsoleFileDirectory + "/Users/server/server.vars.Arma3Profile";
                if (client.Exists(file))
                {
                    var dt = client.GetLastWriteTimeUtc(file);
                    if (dt > last)
                    {
                        var bytes = client.ReadAllBytes(file);
                        backup = PersistBackup.Read(new MemoryStream(bytes), dt);
                    }
                }
                client.Disconnect();
            }
            return backup;
        }
        public async Task Poll()
        {
            var sw = Stopwatch.StartNew();
            var isPolling = Interlocked.Increment(ref pollingLock) > 1;
            try
            {
                if (!isPolling)
                {
                    foreach (var server in await _context.GameServers
                        .Include(s => s.HostServer)
                        .Where(s => s.LastPollUtc < DateTime.UtcNow.AddMinutes(-1) && s.Type == GameServerType.Arma3)
                        .ToListAsync())
                    {
                        var game = _service.GetConfig(server);
                        if (game != null)
                        {
                            await Poll(server, game);
                        }
                    }
                }
            }
            finally
            {
                Interlocked.Decrement(ref pollingLock);
            }
            _logger.LogInformation("Polling took {0} msec, isPolling: {1}", sw.ElapsedMilliseconds, isPolling);
        }

        private async Task Poll(GameServer server, GameConfig config)
        {
            using (var client = _service.GetSftpClient(config.Server))
            {
                client.Connect();

                var actualFiles = client.ListDirectory(config.ConsoleFileDirectory)
                    .Where(f => f.Name.StartsWith(config.ConsoleFilePrefix, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(f => f.LastWriteTimeUtc)
                    .ToList();

                _logger.LogInformation("{0} files to scan", actualFiles.Count);
                foreach (var actualFile in actualFiles)
                {
                    var known = await _context.GameLogFiles.FirstOrDefaultAsync(f => f.GameServerID == server.GameServerID && f.Filename == actualFile.Name);
                    if (known == null || known.ReadSize < actualFile.Length || known.LastSyncUTC < actualFile.LastWriteTimeUtc)
                    {
                        await ProcessArmaLogFile(server, client, actualFile, known ?? new GameLogFile() { Server = server, Filename = actualFile.Name, UnreadData = "" });
                    }
                }

                client.Disconnect();
            }

            server.LastPollUtc = DateTime.UtcNow;
            server.ConnectedPlayers = await _context.GameLogEvents.CountAsync(e => e.Type == GameLogEventType.Connect && !e.IsFinished && e.GameServerID == server.GameServerID);

            _context.Update(server);
            await _context.SaveChangesAsync();
        }

        private async Task ProcessArmaLogFile(GameServer server, SftpClient client, SftpFile actualFile, GameLogFile logFileInfos)
        {
            var data = await ReadData(client, actualFile, logFileInfos);
            logFileInfos.LastSyncUTC = DateTime.UtcNow;
            if (logFileInfos.GameLogFileID == 0)
            {
                _context.Add(logFileInfos);
            }
            else
            {
                _context.Update(logFileInfos);
            }

            var playerConnectEvents = await _context.GameLogEvents
                .Where(e => e.Type == GameLogEventType.Connect && !e.IsFinished && e.GameServerID == server.GameServerID)
                .ToDictionaryAsync(e => e.SteamId, e => e);

            var lineMatch = 0;
            foreach (var line in data.Split("\r\n"))
            {
                var match = ArmaLineRegex.Match(line);
                if (match.Success)
                {
                    lineMatch++;
                    var lineData = match.Groups[7].Value;
                    if (lineData == "Starting mission:")
                    {
                        var dt = ParseDateTime(match);
                        foreach (var evData in playerConnectEvents.Values)
                        {
                            Finish(dt, evData);
                        }
                        playerConnectEvents = new Dictionary<string, GameLogEvent>();
                    }
                    else if (lineData.StartsWith("MP::C ") || lineData.StartsWith("MP::D "))
                    {
                        var tokens = lineData.Substring(6).Split('|');
                        if (tokens.Length == 2 && tokens[0].Length > 15)
                        {
                            var evData = new GameLogEvent()
                            {
                                Timestamp = ParseDateTime(match),
                                SteamId = tokens[0],
                                Server = server,
                                Type = lineData.StartsWith("MP::D ") ? GameLogEventType.Disconnect : GameLogEventType.Connect,
                                PlayerName = tokens[1]
                            };
                            PlayerEvent(playerConnectEvents, evData);
                        }
                    }
                    //else if( lineData.StartsWith("OPC DATA") || lineData.StartsWith("OPD DATA"))
                    //{
                    //    var ev = AramaSteamIdQuotedRegex.Match(lineData);
                    //    if (ev.Success)
                    //    {
                    //        var evData = new GameLogEvent()
                    //        {
                    //            Timestamp = ParseDateTime(match),
                    //            SteamId = ev.Groups[1].Value,
                    //            Server = server,
                    //            Type = lineData.StartsWith("OPD DATA") ? GameLogEventType.Disconnect : GameLogEventType.Connect
                    //        };
                    //        PlayerEvent(playerConnectEvents, evData);
                    //    }
                    //}
                    //else if (lineData.StartsWith("[GTD] (persistence)"))
                    //{
                    //    var ev = ArmaConnectRegex.Match(lineData);
                    //    if (ev.Success)
                    //    {
                    //        var evData = new GameLogEvent()
                    //        {
                    //            Timestamp = ParseDateTime(match),
                    //            SteamId = ev.Groups[2].Value,
                    //            Server = server,
                    //            Type = ev.Groups[1].Value == "Saved" ? GameLogEventType.Disconnect : GameLogEventType.Connect
                    //        };
                    //        PlayerEvent(playerConnectEvents, evData);
                    //    }
                    //}
                }
            }
            _logger.LogInformation("{0} matching lines", lineMatch);
            await _context.SaveChangesAsync();
        }

        private void Finish(DateTime dt, GameLogEvent evData)
        {
            evData.IsFinished = true;
            evData.Duration = dt - evData.Timestamp;
            if (evData.GameLogEventID != 0)
            {
                _context.Update(evData);
            }
        }

        private void PlayerEvent(Dictionary<string, GameLogEvent> dict, GameLogEvent evData)
        {
            GameLogEvent st;
            if (dict.TryGetValue(evData.SteamId, out st))
            {
                if (evData.Type == GameLogEventType.Disconnect)
                {
                    _context.GameLogEvents.Add(evData);
                    Finish(evData.Timestamp, st); 
                    dict.Remove(evData.SteamId);
                }
            }
            else
            {
                if (evData.Type == GameLogEventType.Connect)
                {
                    _context.GameLogEvents.Add(evData);
                    dict[evData.SteamId] = evData;
                }
            }
        }

        private static DateTime ParseDateTime(Match match)
        {
            return  new DateTime(Parse(match.Groups[1].Value), Parse(match.Groups[2].Value), Parse(match.Groups[3].Value),
                                 Parse(match.Groups[4].Value), Parse(match.Groups[5].Value), Parse(match.Groups[6].Value));
        }

        private static int Parse(string value)
        {
            return int.Parse(value.Trim(), CultureInfo.InvariantCulture);
        }

        private async Task<string> ReadData(SftpClient client, SftpFile actualFile, GameLogFile known)
        {
            _logger.LogInformation("Read {0} from offset {1}", actualFile.FullName, known.ReadSize);

            using (var stream = client.Open(actualFile.FullName, FileMode.Open, FileAccess.Read))
            {
                if (known.ReadSize != 0)
                {
                    stream.Seek(known.ReadSize, SeekOrigin.Begin);
                }
                var data = known.UnreadData + await ReadData(stream);
                var dataEnd = data.LastIndexOf("\r\n");
                known.UnreadData = data.Substring(dataEnd + 2);
                known.ReadSize = stream.Position;
                return data.Substring(0, dataEnd);
            }
        }

        private const int maxRead = 1024 * 1024 * 10;

        private static async Task<string> ReadData(SftpFileStream stream)
        {
            var dataToRead = stream.Length - stream.Position;
            using (var reader = new StreamReader(stream, leaveOpen: true))
            {
                if (dataToRead > maxRead)
                {
                    var buffer = new char[maxRead];
                    var read = await reader.ReadAsync(buffer, 0, maxRead);
                    return new string(buffer, 0, read);
                }
                return await reader.ReadToEndAsync();
            }
        }
    }
}
