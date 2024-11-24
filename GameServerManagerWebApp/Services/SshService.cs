using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GameServerManagerWebApp.Entites;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Renci.SshNet;

namespace GameServerManagerWebApp.Services
{
    public class SshService : ISshService, IDisposable
    {
        private readonly List<SshServiceEntry> entries = new List<SshServiceEntry>();
        private readonly ILogger<SshService> _logger;
        private readonly IConfiguration _config;

        public SshService(ILogger<SshService> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        public void Dispose()
        {
            lock (entries)
            {
                foreach (var entry in entries)
                {
                    entry.Dispose();
                }
            }
        }

        public Task<SshCommand> RunCommandAsync(HostServer server, string commandText, CancellationToken cancellationToken = default)
        {
            return GetEntry(server).RunCommandAsync(commandText, cancellationToken);
        }

        public Task RunSftpAsync(HostServer server, Func<SftpClient, Task> operation, CancellationToken cancellationToken = default)
        {
            return GetEntry(server).RunSftpAsync(operation, cancellationToken);
        }

        public Task<TResult> RunSftpAsync<TResult>(HostServer server, Func<SftpClient, Task<TResult>> operation, CancellationToken cancellationToken = default)
        {
            return GetEntry(server).RunSftpAsync(operation, cancellationToken);
        }

        private SshServiceEntry GetEntry(HostServer server)
        {
            lock (entries)
            {
                var entry = entries.FirstOrDefault(e => e.Address == server.Address && e.SshUserName == server.SshUserName);
                if (entry == null)
                {
                    entries.Add(entry = new SshServiceEntry(server, GetPassword(server)));
                }
                return entry;
            }
        }

        private string GetPassword(HostServer server)
        {
            var servers = _config.GetSection("Servers");
            var entry = servers.GetSection(server.Address);
            if (entry == null)
            {
                throw new ArgumentOutOfRangeException();
            }
            return entry.Value;
        }

    }
}
