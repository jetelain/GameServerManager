using System;
using System.Collections.Generic;

namespace GameServerManagerWebApp.Entites
{
    public class GameServer
    {
        public int GameServerID { get; set; }

        public int? HostServerID { get; set; }
        public HostServer HostServer { get; set; }

        public GameServerType Type { get; set; }

        public string Label { get; set; }

        public string Address { get; set; }

        public short Port { get; set; }

        public string UserName { get; set; }

        public string BasePath { get; set; }

        public DateTime LastPollUtc { get; set; }

        public int ConnectedPlayers { get; set; }

        public List<GameServerSyncedFile> SyncFiles { get; set; }
        public List<GameServerConfiguration> Configurations { get; set; }
    }
}
