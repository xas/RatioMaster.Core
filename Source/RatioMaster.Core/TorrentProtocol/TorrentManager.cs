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
        public void CreateTorrentClient(string name)
        {
            Client = AbstractClient.CreateFromName(name);
        }


        public TorrentManager()
        {
            Info = new TorrentInfo();
        }
    }
}
