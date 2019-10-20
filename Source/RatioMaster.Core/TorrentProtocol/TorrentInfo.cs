using System;

namespace RatioMaster.Core.TorrentProtocol
{
    public class TorrentInfo
    {
        private readonly Random random;

        public long Downloaded { get; set; }
        public long DownloadRate { get; set; }
        public string Filename { get; set; }
        public string Hash { get; set; }
        public int Interval { get; set; }
        public string Key { get; set; }
        public long Left { get; set; }
        public string NumberOfPeers { get; set; }
        public string PeerID { get; set; }
        public int Port { get; set; }
        public long Totalsize { get; set; }
        public string Tracker { get; set; }
        public long Uploaded { get; set; }
        public long UploadRate { get; set; }
        public Uri TrackerUri { get; set; }

        public TorrentInfo()
        {
            Uploaded = 0;
            Downloaded = 0;
            Tracker = string.Empty;
            Hash = string.Empty;
            Left = 10000;
            Totalsize = 10000;
            Filename = string.Empty;
            UploadRate = 60 * 1024;
            DownloadRate = 30 * 1024;
            Interval = 1800;
            random = new Random();
            Key = random.Next(1000).ToString();
            Port = random.Next(1025, 65535);
            NumberOfPeers = "200";
            PeerID = string.Empty;
            TrackerUri = null;
        }
    }
}
