using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameServerManagerWebApp.Entites;

#nullable enable

namespace GameServerManagerWebApp.Services.Arma3Mods
{
    public class ArmaModManager : IArmaModManager
    {
        private readonly List<HostArmaMods> entries = new List<HostArmaMods>();
        private readonly ISshService sshService;

        public ArmaModManager(ISshService sshService)
        {
            this.sshService = sshService;
        }

        private HostArmaMods GetEntry(HostServer server)
        {
            lock (entries)
            {
                var entry = entries.FirstOrDefault(e => e.Address == server.Address && e.SshUserName == server.SshUserName);
                if (entry == null)
                {
                    entries.Add(entry = new HostArmaMods(server));
                }
                return entry;
            }
        }

        public bool IsInstalling(HostServer server)
        {
            return GetEntry(server).IsInstalling;
        }

        public ModsInstallResult? GetLastInstallResult(HostServer server)
        {
            return GetEntry(server).GetLastInstallResult();
        }

        public Task RequestInstall(HostServer server)
        {
            return GetEntry(server).RequestModsInstall(sshService);
        }

        public Task<ModsAddResult> Add(HostServer server, List<string> modList)
        {
            return GetEntry(server).AddMods(sshService, modList);
        }

        public Task<bool> RemoveFromList(HostServer server, List<string> modList)
        {
            return GetEntry(server).RemoveModsFromList(sshService, modList);
        }

        public Task<bool> RemoveDuplicates(HostServer server)
        {
            return GetEntry(server).RemoveDuplicates(sshService);
        }

        public Task<List<InstalledMod>> GetInstalledMods(HostServer server)
        {
            return GetEntry(server).GetInstalledMods(sshService);
        }
    }
}
