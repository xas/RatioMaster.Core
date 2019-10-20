using RatioMaster.Core.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace RatioMaster.Core.TorrentProtocol
{
    public class TorrentManager
    {
        public TorrentInfo Info { get; private set; }
        public IClient Client { get; private set; }
        public TrackerResponse Tracker { get; private set; }
        public TorrentFile File { get; private set; }

        public int Seeders { get; private set; }
        public int Leechers { get; private set; }
        public bool IsScrapeUpdated { get; private set; }
        public bool HasInitialPeers { get; private set; }

        public TorrentManager()
        {
            Info = new TorrentInfo();
        }

        public void CreateTorrentClient(string name)
        {
            Client = AbstractClient.CreateFromName(name);
        }

        public void CreateTorrentFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentNullException("TorrentManager:TorrentFileName");
            }
            File = new TorrentFile(fileName);
            Info = new TorrentInfo();
            Info.Tracker = File.Announce;
            Info.TrackerUri = new Uri(File.Announce);
            Info.Hash = File.InfoHash;
            IsScrapeUpdated = false;
            HasInitialPeers = false;
        }

    }
}
