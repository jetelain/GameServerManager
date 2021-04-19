using GameServerManagerWebApp.Entites;

namespace GameServerManagerWebApp.Services
{
    public class GameConfig
    {
        public string Name { get; set; }
        public string StartCmd { get; set; }
        public string StopCmd { get; set; }
        public string[] ConfigFiles { get; set; }
        public string MissionDirectory { get; set; }
        public string ConsoleFile { get; internal set; }
        public string[] Command { get; internal set; }
        public string TopUserName { get; internal set; }
        public string[] OtherUsers { get; internal set; }
        public HostServer Server { get; internal set; }
        public string GameBaseDirectory { get; internal set; }
        public string ConsoleFileDirectory { get; internal set; }
        public string ConsoleFilePrefix { get; internal set; }
    }
}
