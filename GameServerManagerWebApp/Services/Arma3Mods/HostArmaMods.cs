using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GameServerManagerWebApp.Entites;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.VisualBasic;
using Renci.SshNet;

#nullable enable

namespace GameServerManagerWebApp.Services.Arma3Mods
{
    internal class HostArmaMods
    {
        private readonly SemaphoreSlim installSemaphore = new SemaphoreSlim(1, 1);
        private Task<ModsInstallResult>? install;

        public HostArmaMods(HostServer server)
        {
            Address = server.Address;
            SshUserName = server.SshUserName;
        }

        public string Address { get; }

        public string SshUserName { get; }

        public bool IsInstalling => install != null && !install.IsCompleted;

        public ModsInstallResult? GetLastInstallResult()
        {
            if (install == null || !install.IsCompleted)
            {
                return null;
            }
            if (install.IsFaulted)
            {
                return new ModsInstallResult(DateTime.UtcNow, DateTime.UtcNow, -1, install.Exception?.Message);
            }
            return install.Result;
        }

        public async Task RequestModsInstall(ISshService sshService)
        {
            await installSemaphore.WaitAsync();
            try
            {
                if (install != null && !install.IsCompleted)
                {
                    return;
                }
                install = DoInstall(sshService);
            }
            finally
            {
                installSemaphore.Release();
            }
        }

        private async Task<ModsInstallResult> DoInstall(ISshService sshService)
        {
            var started = DateTime.UtcNow;

            var result = await sshService.RunLongCommandAsync(new HostServer { Address = Address, SshUserName = SshUserName }, "sudo -H -u arma3-mods /home/arma3-mods/update.sh");

            var finished = DateTime.UtcNow;

            return new ModsInstallResult(started, finished, result.ExitStatus, result.Result);
        }

        private static readonly Regex regex = new Regex(@"workshop_download_item 107410 ([0-9]+)", RegexOptions.Compiled);
        private static readonly Regex regexName = new Regex(@"/([0-9]+)/meta.cpp:[ ]*name[ ]*=[ ]*""(.*)""", RegexOptions.Compiled);

        public async Task<List<InstalledMod>> GetInstalledMods(ISshService sshService)
        {
            var server = new HostServer { Address = Address, SshUserName = SshUserName };

            var steamIds = new List<string>();

            var mods = new List<InstalledMod>();

            var lines = await sshService.RunSftpAsync(server, async sftp => sftp.ReadAllLines("/home/arma3-mods/arma3-mods.txt"));

            foreach (var line in lines)
            {
                var match = regex.Match(line);
                if (match.Success)
                {
                    steamIds.Add(match.Groups[1].Value);
                }
            }

            var duCommand = await sshService.RunCommandAsync(server, "du -b -s /home/arma3-mods/steamapps/workshop/content/107410/*");
            var sizePerMod = duCommand.Result.Split('\n').Select(s => s.Split('\t')).Where(s => s.Length > 1).Select(s => new { Size = long.Parse(s[0]), Mod = s[1].Split('/').Last() }).ToList();

            var grepCommand = await sshService.RunCommandAsync(server, "grep -o -e \"name = \\\".*\\\"\" /home/arma3-mods/steamapps/workshop/content/107410/*/*.cpp");
            var namePerMod = grepCommand.Result.Split('\n').Select(s => regexName.Match(s)).Where(s => s.Success).Select(s => new { Label = s.Groups[2].Value, Mod = s.Groups[1].Value }).ToList();

            foreach (var modSteamId in steamIds)
            {
                var modSize = sizePerMod.FirstOrDefault(m => m.Mod == modSteamId)?.Size ?? 0L;
                var modName = namePerMod.FirstOrDefault(m => m.Mod == modSteamId)?.Label ?? modSteamId;

                mods.Add(new InstalledMod(modSteamId, modSize, modName));
            }

            return mods;
        }

        public async Task<ModsAddResult> AddMods(ISshService sshService, List<string> steamIds)
        {
            if (steamIds.Any(s => !long.TryParse(s, out _)))
            {
                throw new ArgumentException("Invalid steamIds");
            }

            var added = new List<string>();
            var existing = new List<string>();

            await installSemaphore.WaitAsync();
            try
            {
                if (install != null && !install.IsCompleted)
                {
                    return new ModsAddResult(added, existing); // already installing
                }

                await sshService.RunSftpAsync(new HostServer { Address = Address, SshUserName = SshUserName }, async sftp =>
                {
                    var lines = sftp.ReadAllLines("/home/arma3-mods/arma3-mods.txt");
                    var alreadyInstalled = lines.Select(l => regex.Match(l)).Where(l => l.Success).Select(l => l.Groups[1].Value).ToHashSet();
                    var beforeQuit = lines.TakeWhile(lines => !lines.Contains("quit"));
                    var quitAndAfter = lines.Skip(beforeQuit.Count());
                    existing.AddRange(steamIds.Where(id => alreadyInstalled.Contains(id)));
                    var toAdd = steamIds.Where(id => !alreadyInstalled.Contains(id)).ToList();
                    if (toAdd.Count == 0)
                    {
                        return;
                    }
                    var newLines = beforeQuit.Concat(toAdd.Select(id => $"workshop_download_item 107410 {id}")).Concat(quitAndAfter);
                    WriteAllLines(sftp, newLines);
                    added.AddRange(toAdd);
                });

                if (added.Count > 0)
                {
                    // request the install
                    install = DoInstall(sshService);
                }
            }
            finally
            {
                installSemaphore.Release();
            }
            return new ModsAddResult(added, existing); // already installing
        }

        public async Task<bool> RemoveModsFromList(ISshService sshService, List<string> steamIds)
        {
            if (steamIds.Any(s => !long.TryParse(s, out _)))
            {
                throw new ArgumentException("Invalid steamIds");
            }

            await installSemaphore.WaitAsync();
            try
            {
                if (install != null && !install.IsCompleted)
                {
                    return false;
                }

                await sshService.RunSftpAsync(new HostServer { Address = Address, SshUserName = SshUserName }, async sftp =>
                {
                    var lines = sftp.ReadAllLines("/home/arma3-mods/arma3-mods.txt");

                    var newLines = lines.Where(line =>
                    {
                        var match = regex.Match(line);
                        return !match.Success || !steamIds.Contains(match.Groups[1].Value);
                    }).ToList();

                    WriteAllLines(sftp, newLines);
                });
            }
            finally
            {
                installSemaphore.Release();
            }
            return true; // already installing
        }

        public async Task<bool> RemoveDuplicates(ISshService sshService)
        {
            await installSemaphore.WaitAsync();
            try
            {
                if (install != null && !install.IsCompleted)
                {
                    return false;
                }

                await sshService.RunSftpAsync(new HostServer { Address = Address, SshUserName = SshUserName }, async sftp =>
                {
                    var ok = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var lines = sftp.ReadAllLines("/home/arma3-mods/arma3-mods.txt");
                    var newLines = lines.Where(line => ok.Add(line)).ToList();
                    WriteAllLines(sftp, newLines);
                });
            }
            finally
            {
                installSemaphore.Release();
            }
            return true; // already installing
        }

        private static void WriteAllLines(SftpClient sftp, IEnumerable<string> contents)
        {
            using var stream = sftp.Open("/home/arma3-mods/arma3-mods.txt", FileMode.Create);
            using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            foreach (var line in contents)
            {
                writer.WriteLine(line);
            }
        }

        internal async Task<bool> Uninstall(ISshService sshService, IEnumerable<long> modList)
        {
            await installSemaphore.WaitAsync();
            try
            {
                if (install != null && !install.IsCompleted)
                {
                    return false;
                }

                var hostServer = new HostServer { Address = Address, SshUserName = SshUserName };

                var steamIds = modList.Select(m => m.ToString()).ToHashSet();

                await sshService.RunSftpAsync(hostServer, async sftp =>
                {
                    var lines = sftp.ReadAllLines("/home/arma3-mods/arma3-mods.txt");

                    var newLines = lines.Where(line =>
                    {
                        var match = regex.Match(line);
                        return !match.Success || !steamIds.Contains(match.Groups[1].Value);
                    }).ToList();

                    WriteAllLines(sftp, newLines);
                });

                foreach (var mod in modList)
                {
                    var result = await sshService.RunCommandAsync(hostServer, $"rm -rf /home/arma3-mods/steamapps/workshop/content/107410/{mod}");
                }
            }
            finally
            {
                installSemaphore.Release();
            }
            return true;
        }
    }
}
