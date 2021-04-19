using System.Collections.Generic;
using GameServerManagerWebApp.Entites;
using GameServerManagerWebApp.Services;

namespace GameServerManagerWebApp.Models
{
    public class SetupArma3ModsetVM
    {
        public GameConfig Game { get; internal set; }
        public List<SetupArma3Mod> Mods { get; internal set; }
        public bool IsSetupOK { get; internal set; }
        public bool IsSetupFailed { get; internal set; }
        public List<Modset> Modsets { get; internal set; }
        public int? ModsetID { get; internal set; }
    }
}
