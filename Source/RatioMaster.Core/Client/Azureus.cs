using System;
using System.Collections.Generic;
using System.Text;

namespace RatioMaster.Core.Client
{
    public class Azureus : AbstractClient
    {
        public Azureus() : base()
        {
            Name = "Azureus 3.1.1.0";
            HttpProtocol = "HTTP/1.1";
            HashUpperCase = true;
            Key = GenerateIdString("alphanumeric", 8, false, false);
            Headers = "User-Agent: Azureus 3.1.1.0;Windows XP;Java 1.6.0_07\r\nConnection: close\r\nAccept-Encoding: gzip\r\nHost: {host}\r\nAccept: text/html, image/gif, image/jpeg, *; q=.2, */*; q=.2\r\n";
            PeerID = "-AZ3110-" + GenerateIdString("alphanumeric", 12, false, false);
            Query = "info_hash={infohash}&peer_id={peerid}&supportcrypto=1&port={port}&azudp={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&corrupt=0{event}&numwant={numwant}&no_peer_id=1&compact=1&key={key}&azver=3";
            DefNumWant = 50;
            Parse = true;
            SearchString = "&peer_id=-AZ3110-";
            ProcessName = "Azureus";
            StartOffset = 0;
            MaxOffset = 100000000;
        }
    }
}
