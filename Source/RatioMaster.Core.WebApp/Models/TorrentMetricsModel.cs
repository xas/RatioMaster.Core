namespace RatioMaster.Core.WebApp.Models
{
    public class TorrentMetricsModel
    {
        public string Uploaded { get; set; }
        public string Downloaded { get; set; }
        public string Seeders { get; set; }
        public string Leechers { get; set; }
        public string TotalTime { get; set; }
    }
}
