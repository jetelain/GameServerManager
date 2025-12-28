using System.Collections.Generic;
using GameServerManagerWebApp.Entites;
using GameServerManagerWebApp.Services;

namespace GameServerManagerWebApp.Models
{
    public class GameServerInfo
    {
        public string Name { get; set; }
        public bool IsRunning { get; set; }
        public decimal Cpu { get; set; }
        public decimal Mem { get; set; }
        public List<ProcessInfo> Processes { get; set; }
        public GameServer GameServer { get; internal set; }
        public bool IsUpdating { get; internal set; }
        public InstallResult LastUpdateResult { get; internal set; }
    }
}