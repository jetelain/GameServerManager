using System;

namespace GameServerManagerWebApp.Services
{
    public class InstallResult
    {
        public InstallResult(DateTime started, DateTime finished, int exitStatus, string result)
        {
            Started = started;
            Finished = finished;
            ExitStatus = exitStatus;
            Result = result;
        }

        public DateTime Started { get; }

        public DateTime Finished { get; }

        public TimeSpan Duration => Finished - Started;

        public int ExitStatus { get; }

        public string Result { get; }
    }
}
