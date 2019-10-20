using System;

namespace BitTorrent
{
    public class IncompleteTorrentData : TorrentException
    {
        public IncompleteTorrentData(string message)
            : base(message)
        {
        }
    }

    public class TorrentException : Exception
    {
        public TorrentException(string message)
            : base(message)
        {
        }
    }
}