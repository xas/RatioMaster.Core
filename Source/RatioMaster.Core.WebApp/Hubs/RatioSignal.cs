using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RatioMaster.Core.WebApp.Hubs
{
    public class RatioSignal : Hub<ISignalHub>
    {
        // A new client connected
        // Send current information
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }
    }
}
