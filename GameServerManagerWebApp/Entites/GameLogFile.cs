using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameServerManagerWebApp.Entites
{
    public class GameLogFile
    {
        public int GameLogFileID { get; set; }

        public int GameServerID { get; set; }

        public GameServer Server { get; set; }

        public string Filename { get; set; }

        public DateTime LastSyncUTC { get; set; }

        public long ReadSize { get; set; }

        public string UnreadData { get; set; }
    }
}
