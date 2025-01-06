using System.Collections.Generic;
using GameServerManagerWebApp.Entites;
using GameServerManagerWebApp.Services.Arma3Mods;

namespace GameServerManagerWebApp.Models
{
    internal class HostModsViewModel
    {
        public HostModsViewModel(HostServer server)
        {
            Server = server;
        }

        public bool IsInstalling { get; internal set; }
        public HostServer Server { get; }
        public List<InstalledMod> Mods { get; internal set; }
        public ModsInstallResult LastInstall { get; internal set; }
    }
}