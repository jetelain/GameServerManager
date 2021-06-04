using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameServerManagerWebApp.Entites
{
    public class GamePersistSnapshot
    {
        public int GamePersistSnapshotID { get; set; }

        public int GameServerID { get; set; }
        public GameServer GameServer { get; set; }

        public DateTime Timestamp { get; set; }

        public string GamePersistName { get; set; }

        public string Backup { get; set; }
    }
}
