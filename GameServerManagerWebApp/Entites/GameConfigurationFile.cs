using System;

namespace GameServerManagerWebApp.Entites
{
    public class GameConfigurationFile
    {
        public int GameConfigurationFileID { get; set; }

        public int GameServerConfigurationID { get; set; }
        public GameServerConfiguration Configuration { get; set; }

        public string Path { get; set; }

        public string Content { get; set; }

        public DateTime LastChangeUTC { get; set; }
    }
}
