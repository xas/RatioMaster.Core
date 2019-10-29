using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RatioMaster.Core.WebApp.Hubs
{
    public interface ISignalHub
    {
        Task UpdateMetrics();
    }
}
