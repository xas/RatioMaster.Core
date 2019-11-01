using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace RatioMaster.Core.WebApp.Hubs
{
    public class BackgroundServiceWrapper<T> : IHostedService where T : IHostedService
    {
        readonly T backgroundService;

        public BackgroundServiceWrapper(T backgroundService)
        {
            this.backgroundService = backgroundService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return backgroundService.StartAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return backgroundService.StopAsync(cancellationToken);
        }
    }
}
