using System;
using System.Collections.Generic;
using System.Text;

namespace RatioMaster.Core.Client
{
    public class Transmission : AbstractClient
    {
        public Transmission() : base()
        {
            Name = "Transmission 2.92 (14714)";
            HttpProtocol = "HTTP/1.1";
            HashUpperCase = false;
            Key = GenerateIdString("hex", 8, false, true);
            Headers = "User-Agent: Transmission/2.92\r\nHost: {host}\r\nAccept: */*\r\nAccept-Encoding: gzip;q=1.0, deflate, identity\r\n";
            PeerID = "-TR2920-" + GenerateIdString("alphanumeric", 12, false, false);
            Query = "info_hash={infohash}&peer_id={peerid}&port={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&numwant={numwant}&key={key}&compact=1&supportcrypto=1{event}";
            DefNumWant = 80;
            SearchString = "&peer_id=-TR2920-";
            ProcessName = "Transmission";
        }
    }
}
