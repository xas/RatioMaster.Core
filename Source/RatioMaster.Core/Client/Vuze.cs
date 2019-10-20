using System;
using System.Collections.Generic;
using System.Text;

namespace RatioMaster.Core.Client
{
    public class Vuze : AbstractClient
    {
        public Vuze() : base()
        {
            Name = "Vuze 4.2.0.8";
            HttpProtocol = "HTTP/1.1";
            HashUpperCase = true;
            Key = GenerateIdString("alphanumeric", 8, false, false);
            Headers = "User-Agent: Azureus 4.2.0.8;Windows XP;Java 1.6.0_05\r\nConnection: close\r\nAccept-Encoding: gzip\r\nHost: {host}\r\nAccept: text/html, image/gif, image/jpeg, *; q=.2, */*; q=.2\r\n";
            PeerID = "-AZ4208-" + GenerateIdString("alphanumeric", 12, false, false);
            Query = "info_hash={infohash}&peer_id={peerid}&supportcrypto=1&port={port}&azudp={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&corrupt=0{event}&numwant={numwant}&no_peer_id=1&compact=1&key={key}&azver=3";
            DefNumWant = 50;
            Parse = true;
            SearchString = "&peer_id=-AZ4208-";
            ProcessName = "azureus";
            StartOffset = 0;
            MaxOffset = 100000000;
        }
    }
}
