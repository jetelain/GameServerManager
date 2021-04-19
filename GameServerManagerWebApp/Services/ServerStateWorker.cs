using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GameServerManagerWebApp.Services
{
    public class ServerStateWorker : IHostedService, IDisposable
    {
        private Timer _timer;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ServerStateWorker> _logger;

        public ServerStateWorker(ILogger<ServerStateWorker> logger, IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Server State Worker running.");
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(10));
            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            var h = DateTime.Now.Hour;
            if (h < 2 || h > 18 || (h > 15 && DateTime.Now.DayOfWeek == DayOfWeek.Sunday))
            {
                _logger.LogInformation("Server State Worker Poll.");
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        scope.ServiceProvider.GetRequiredService<ServerStateService>().Poll().Wait();
                    }
                }
                catch(Exception e)
                {
                    _logger.LogError(e, "Poll failed.");
                }
            }
            //if (h == 6 && DateTime.Now.Minute < 11)
            //{
            //    _logger.LogInformation("Update inventory.");
            //    try
            //    {
            //        using (var scope = _scopeFactory.CreateScope())
            //        {
            //            scope.ServiceProvider.GetRequiredService<ServerStateService>().UpdateInventory().Wait();
            //        }
            //    }
            //    catch (Exception e)
            //    {
            //        _logger.LogError(e, "Inventory failed.");
            //    }
            //}
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Server State Worker is stopping.");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
