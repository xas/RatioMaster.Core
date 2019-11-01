using RatioMaster.Core.WebApp.Models;
using System.Threading.Tasks;

namespace RatioMaster.Core.WebApp.Hubs
{
    public interface ISignalHub
    {
        Task UpdateMetrics(TorrentMetricsModel model);
        Task SendTorrentInfo(TorrentInfoModel model);
    }
}
