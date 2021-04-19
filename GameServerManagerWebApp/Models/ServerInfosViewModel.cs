using System.Collections.Generic;
using GameServerManagerWebApp.Entites;
using GameServerManagerWebApp.Services;

namespace GameServerManagerWebApp.Models
{
    public class ServerInfosViewModel
    {
        public GameServerInfo Infos { get; internal set; }
        public List<ConfigFileInfos> ConfigFiles { get; internal set; }
        public List<string> MissionFiles { get; internal set; }
        public string Console { get; internal set; }
        public GameConfig Game { get; internal set; }
        public List<ModInfoVM> Mods { get; internal set; }
        public GameServerConfiguration CurrentConfig { get; internal set; }
        public GameServer GameServer { get; internal set; }
    }
}
