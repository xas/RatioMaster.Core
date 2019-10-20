using System;
using System.Collections.Generic;
using System.Text;

namespace RatioMaster.Core.Client
{
    public class BitTorrent : AbstractClient
    {
        public BitTorrent() : base()
        {
            Name = "BitTorrent 6.0.3 (8642)";
            HttpProtocol = "HTTP/1.1";
            HashUpperCase = false;
            Key = GenerateIdString("hex", 8, false, true);
            Headers = "Host: {host}\r\nUser-Agent: BitTorrent/6030\r\nAccept-Encoding: gzip\r\n";
            PeerID = "M6-0-3--" + GenerateIdString("random", 12, true, false);
            Query = "info_hash={infohash}&peer_id={peerid}&port={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&key={key}{event}&numwant={numwant}&compact=1&no_peer_id=1";
            DefNumWant = 200;
            Parse = false;
            SearchString = "";
            ProcessName = "bittorrent";
            StartOffset = 0;
            MaxOffset = 100000000;
        }
    }
}
