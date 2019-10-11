namespace BitTorrent
{
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Security.Cryptography;

    public class IncompleteTorrentData : TorrentException
    {
        public IncompleteTorrentData(string message)
            : base(message)
        {
        }
    }
}