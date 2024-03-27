using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using GameServerManagerWebApp.Models;

namespace GameServerManagerWebApp.Entites
{
    public class Modset
    {
        public int ModsetID { get; set; }

        public GameServerType GameType { get; set; }

        public string AccessToken { get; set; }
        public string Label { get; set; }
        public string Name { get; set; }
        public int Count { get; set; }
        public string DefinitionFile { get; set; }
        public string ConfigurationFile { get; set; }
        public DateTime LastUpdate { get; internal set; }

        [NotMapped]
        public List<ModesetGameServerMods> Servers { get; internal set; }
    }
}
