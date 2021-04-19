using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameServerManagerWebApp.Entites
{
    public class GameServerSyncedFile
    {
        public int GameServerSyncedFileID { get; set; }

        public int GameServerID { get; set; }
        public GameServer GameServer { get; set; }

        public string Path { get; set; }
        public string SyncUri { get; set; }

        public string Content { get; set; }

        public DateTime LastChangeUTC { get; set; }
    }
}
