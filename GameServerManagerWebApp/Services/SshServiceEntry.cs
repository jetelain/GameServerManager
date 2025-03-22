using System;
using System.Threading;
using System.Threading.Tasks;
using GameServerManagerWebApp.Entites;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace GameServerManagerWebApp.Services
{
    internal class SshServiceEntry : IDisposable
    {
        private readonly SemaphoreSlim sshSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim sftpSemaphore = new SemaphoreSlim(1, 1);
        private SshClient? sshClient;
        private SftpClient? sftpClient;
        private readonly string password;

        public SshServiceEntry(HostServer server, string password)
        {
            this.Address = server.Address;
            this.SshUserName = server.SshUserName;
            this.password = password;
        }

        public string Address { get; set; }

        public string SshUserName { get; set; }

        internal async Task<SshCommand> RunCommandAsync(string commandText, CancellationToken cancellationToken)
        {
            await sshSemaphore.WaitAsync();
            try
            {
                return (await ConnectSshClient(cancellationToken)).RunCommand(commandText);
            }
            catch (SshOperationTimeoutException)
            {
                // Try again once
                sshClient?.Dispose();
                sftpClient = null;
                return (await ConnectSshClient(cancellationToken)).RunCommand(commandText);
            }
            finally
            {
                sshSemaphore.Release();
            }
        }

        private async Task<SshClient> ConnectSshClient(CancellationToken cancellationToken)
        {
            if (sshClient == null)
            {
                sshClient = new SshClient(Address, SshUserName, password);
                sshClient.ConnectionInfo.Timeout = TimeSpan.FromSeconds(1);
            }
            var attempt = 0;
            while (!sshClient.IsConnected)
            {
                try
                {
                    await sshClient.ConnectAsync(cancellationToken);
                }
                catch
                {
                    attempt++;
                    if (attempt == 10)
                    {
                        sshClient = null;
                        throw;
                    }
                }
            }
            return sshClient;
        }

        public void Dispose()
        {
            sshClient?.Dispose();
        }

        internal async Task RunSftpAsync(Func<SftpClient, Task> operation, CancellationToken cancellationToken)
        {
            await sftpSemaphore.WaitAsync();
            try
            {
                await operation(await ConnectSftpClient(cancellationToken));
            }
            finally
            {
                sftpSemaphore.Release();
            }
        }

        internal async Task<TResult> RunSftpAsync<TResult>(Func<SftpClient, Task<TResult>> operation, CancellationToken cancellationToken)
        {
            await sftpSemaphore.WaitAsync();
            try
            {
                return await operation(await ConnectSftpClient(cancellationToken));
            }
            finally
            {
                sftpSemaphore.Release();
            }
        }

        private async Task<SftpClient> ConnectSftpClient(CancellationToken cancellationToken)
        {
            if (sftpClient == null)
            {
                sftpClient = new SftpClient(Address, SshUserName, password);
                sftpClient.ConnectionInfo.Timeout = TimeSpan.FromSeconds(1);
            }
            var attempt = 0;
            while (!sftpClient.IsConnected)
            {
                try
                {
                    await sftpClient.ConnectAsync(cancellationToken);
                }
                catch
                {
                    attempt++;
                    if (attempt == 10)
                    {
                        sftpClient = null;
                        throw;
                    }
                }
            }
            return sftpClient;
        }
    }
}