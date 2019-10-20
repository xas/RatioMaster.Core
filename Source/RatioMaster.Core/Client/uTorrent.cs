using System;
using System.Collections.Generic;
using System.Text;

namespace RatioMaster.Core.Client
{
    public class uTorrent : AbstractClient
    {
        public uTorrent() : base()
        {
            Name = "uTorrent 3.3.2";
            HttpProtocol = "HTTP/1.1";
            HashUpperCase = false;
            Key = GenerateIdString("hex", 8, false, true);
            Headers = "Host: {host}\r\nUser-Agent: uTorrent/3320\r\nAccept-Encoding: gzip\r\n";
            PeerID = "-UT3320-%18w" + GenerateIdString("random", 10, true, false);
            Query = "info_hash={infohash}&peer_id={peerid}&port={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&corrupt=0&key={key}{event}&numwant={numwant}&compact=1&no_peer_id=1";
            DefNumWant = 200;
            Parse = true;
            SearchString = "&peer_id=-UT3320-";
            ProcessName = "uTorrent";
            StartOffset = 0;
            MaxOffset = 200000000;
        }
    }
}
