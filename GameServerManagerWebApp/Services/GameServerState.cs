using System;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace GameServerManagerWebApp.Services
{
    internal class GameServerState
    {
        public required int GameServerID { get; internal set; }

        internal SemaphoreSlim UpdateSemaphore { get; } = new SemaphoreSlim(1, 1);

        public bool IsUpdating => UpdateSemaphore.CurrentCount == 0;

        internal Task<InstallResult>? UpdateTask { get; set; }

        public InstallResult? GetLastUpdateResult()
        {
            if (UpdateTask == null || !UpdateTask.IsCompleted)
            {
                return null;
            }
            if (UpdateTask.IsFaulted)
            {
                return new InstallResult(DateTime.UtcNow, DateTime.UtcNow, -1, UpdateTask.Exception?.Message);
            }
            return UpdateTask.Result;
        }
    }
}