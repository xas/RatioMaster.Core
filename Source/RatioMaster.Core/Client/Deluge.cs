using System;
using System.Collections.Generic;
using System.Text;

namespace RatioMaster.Core.Client
{
    public class Deluge : AbstractClient
    {
        public Deluge() : base()
        {
            Name = "Deluge 1.2.0";
            HttpProtocol = "HTTP/1.0";
            HashUpperCase = false;
            Key = GenerateIdString("alphanumeric", 8, false, false);
            Headers = "Host: {host}\r\nUser-Agent: Deluge 1.2.0\r\nConnection: close\r\nAccept-Encoding: gzip\r\n";
            PeerID = "-DE1200-" + GenerateIdString("alphanumeric", 12, false, false);
            Query = "info_hash={infohash}&peer_id={peerid}&port={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&event={event}&key={key}&compact=1&numwant={numwant}&supportcrypto=1&no_peer_id=1";
            DefNumWant = 200;
            Parse = false;
            SearchString = "-DE1200-";
            ProcessName = "deluge";
            StartOffset = 0;
            MaxOffset = 100000000;
        }
    }
}
