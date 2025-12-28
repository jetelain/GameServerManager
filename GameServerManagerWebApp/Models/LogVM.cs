using GameServerManagerWebApp.Entites;

namespace GameServerManagerWebApp.Models
{
    public class LogVM
    {
        public GameLogEvent Log { get; set; }

        public string User { get; set; }

        public string Label
        {
            get
            {
                switch (Log.Type)
                {
                    case GameLogEventType.Connect:
                        return $"{User} has connected";
                    case GameLogEventType.Disconnect:
                        return $"{User} has disconnected";
                    case GameLogEventType.ServerStart:
                        return $"{User} has started server";
                    case GameLogEventType.ServerStop:
                        return $"{User} has stopped server";
                    case GameLogEventType.UpdateServer:
                        return $"{User} has updated server";
                    default:
                        return Log.Type.ToString();
                }
            }
        }
    }
}