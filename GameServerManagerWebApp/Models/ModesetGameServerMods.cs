using System.Collections.Generic;
using GameServerManagerWebApp.Entites;

namespace GameServerManagerWebApp.Models
{
    public class ModesetGameServerMods
    {
        public GameServer GameServer { get; set; }

        public List<SetupArma3Mod> Mods { get; set; }
    }
}
