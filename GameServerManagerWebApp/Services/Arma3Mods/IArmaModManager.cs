using System.Collections.Generic;
using System.Threading.Tasks;
using GameServerManagerWebApp.Entites;

#nullable enable

namespace GameServerManagerWebApp.Services.Arma3Mods
{
    public interface IArmaModManager
    {
        bool IsInstalling(HostServer server);

        InstallResult? GetLastInstallResult(HostServer server);

        Task RequestInstall(HostServer server);

        Task RequestInstall(HostServer server, List<long> modList);

        Task<List<InstalledMod>> GetInstalledMods(HostServer server);

        Task<ModsAddResult> Add(HostServer server, List<long> modList);

        Task<bool> RemoveFromList(HostServer server, List<long> modList);

        Task<bool> Uninstall(HostServer server, IEnumerable<long> modList);

        Task<bool> RemoveDuplicates(HostServer server);

        Task ClearLastInstallResult(HostServer server);
    }
}
