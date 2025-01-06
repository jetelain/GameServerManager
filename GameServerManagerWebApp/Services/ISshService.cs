using System;
using System.Threading;
using System.Threading.Tasks;
using GameServerManagerWebApp.Entites;
using Renci.SshNet;

namespace GameServerManagerWebApp.Services
{
    public interface ISshService
    {
        Task<SshCommand> RunCommandAsync(HostServer server, string commandText, CancellationToken cancellationToken = default);

        Task RunSftpAsync(HostServer server, Func<SftpClient, Task> operation, CancellationToken cancellationToken = default);

        Task<TResult> RunSftpAsync<TResult>(HostServer server, Func<SftpClient, Task<TResult>> operation, CancellationToken cancellationToken = default);

        Task<SshCommand> RunLongCommandAsync(HostServer server, string commandText, CancellationToken cancellationToken = default);


    }
}