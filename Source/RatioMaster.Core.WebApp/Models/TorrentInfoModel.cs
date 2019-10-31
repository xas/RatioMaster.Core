namespace RatioMaster.Core.WebApp.Models
{
    public class TorrentInfoModel
    {
        public string FileName { get; set; }
        public string Tracker { get; set; }
        public string Hash { get; set; }
        public string Peers { get; set; }
        public string PeerId { get; set; }
        public string ClientKey { get; set; }
        public string Port { get; set; }
    }
}
