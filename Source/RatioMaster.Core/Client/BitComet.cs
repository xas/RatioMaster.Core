using System;
using System.Collections.Generic;
using System.Text;

namespace RatioMaster.Core.Client
{
    public class BitComet : AbstractClient
    {
        public BitComet() : base()
        {
            Name = "BitComet 1.20";
            HttpProtocol = "HTTP/1.1";
            HashUpperCase = true;
            Key = GenerateIdString("numeric", 5, false, false);
            Headers = "Host: {host}\r\nConnection: close\r\nAccpet: */*\r\nAccept-Encoding: gzip\r\nUser-Agent: BitComet/1.20.3.25\r\nPragma: no-cache\r\nCache-Control: no-cache\r\n";
            PeerID = "-BC0120-" + GenerateIdString("random", 12, true, true);
            Query = "info_hash={infohash}&peer_id={peerid}&port={port}&natmapped=1&localip={localip}&port_type=wan&uploaded={uploaded}&downloaded={downloaded}&left={left}&numwant={numwant}&compact=1&no_peer_id=1&key={key}{event}";
            DefNumWant = 200;
            Parse = true;
            SearchString = "&peer_id=-BC0120-";
            ProcessName = "BitComet";
            StartOffset = 0;
            MaxOffset = 60000000;
        }
    }
}
