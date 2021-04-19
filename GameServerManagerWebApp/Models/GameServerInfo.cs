using System.Collections.Generic;
using GameServerManagerWebApp.Entites;

namespace GameServerManagerWebApp.Models
{
    public class GameServerInfo
    {
        public string Name { get; set; }
        public bool Running { get; set; }
        public decimal Cpu { get; set; }
        public decimal Mem { get; set; }
        public List<ProcessInfo> Processes { get; set; }
        public GameServer GameServer { get; internal set; }
    }
}