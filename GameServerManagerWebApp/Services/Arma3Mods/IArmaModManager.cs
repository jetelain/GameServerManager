using System.Collections.Generic;
using System.Threading.Tasks;
using GameServerManagerWebApp.Entites;

#nullable enable

namespace GameServerManagerWebApp.Services.Arma3Mods
{
    public interface IArmaModManager
    {
        bool IsInstalling(HostServer server);

        ModsInstallResult? GetLastInstallResult(HostServer server);

        Task RequestInstall(HostServer server);

        Task<List<InstalledMod>> GetInstalledMods(HostServer server);

        Task<ModsAddResult> Add(HostServer server, List<string> modList);
    }
}
