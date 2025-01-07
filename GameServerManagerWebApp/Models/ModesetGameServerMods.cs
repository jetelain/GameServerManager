using System.Collections.Generic;
using GameServerManagerWebApp.Entites;
using GameServerManagerWebApp.Services.Arma3Mods;

namespace GameServerManagerWebApp.Models
{
    public class ModesetGameServerMods
    {
        public HostServer HostServer { get; set; }

        public List<ModsetModViewModel> Mods { get; set; }
    }
}
