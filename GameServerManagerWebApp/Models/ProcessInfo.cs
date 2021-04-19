using GameServerManagerWebApp.Entites;

namespace GameServerManagerWebApp.Models
{
    public class ProcessInfo
    {
        public int Pid { get; set; }
        public string User { get; set; }
        public decimal Cpu { get; set; }
        public decimal Mem { get; set; }
        public string Cmd { get; set; }
        public HostServer Server { get; internal set; }
    }
}