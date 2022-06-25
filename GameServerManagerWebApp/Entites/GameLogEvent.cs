using System;

namespace GameServerManagerWebApp.Entites
{
    public class GameLogEvent
    {
        public int GameLogEventID { get; set; }

        public DateTime Timestamp { get; set; }

        public int GameServerID { get; set; }

        public GameServer Server { get; set; }

        public string SteamId { get; set; }
        public string PlayerName { get; set; }

        public GameLogEventType Type { get; set; }

        public TimeSpan? Duration { get; set; }

        public bool IsFinished { get; set; }

        public bool IsAggregated { get; set; }
    }
}
