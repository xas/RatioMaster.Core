using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RatioMaster.Core.WebApp.Hubs
{
    public class RatioHostedService : IHostedService
    {
        private readonly IHubContext<RatioSignal, ISignalHub> RatioHub;
        private bool autostart = false;
        private Timer timer;

        public RatioHostedService(IHubContext<RatioSignal, ISignalHub> _hub)
        {
            RatioHub = _hub;
        }

        public Task UploadFile(CancellationToken cancellationToken, string filePath)
        {
            return Task.CompletedTask;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // This is a hack to disable the autostart process when starting the application for the first time
            if (!autostart)
            {
                autostart = true;
                return Task.CompletedTask;
            }
            // Start the timer to update data
            if (timer == null)
            {
                timer = new Timer(UpdateAsync, null, 1500, 1500);
            }
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (timer != null)
            {
                timer.Change(Timeout.Infinite, Timeout.Infinite);
                timer.Dispose();
                timer = null;
            }
            return Task.CompletedTask;
        }

        private async void UpdateAsync(object state)
        {
            await RatioHub.Clients.All.UpdateMetrics();
        }
    }
}
