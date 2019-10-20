using System.Net;

namespace RatioMaster.Core.TorrentProtocol
{
    public class Peer
    {
        public IPAddress IpAddress;

        public string PeerID;

        public ushort Port;

        public Peer(byte[] ip, short port)
        {
            PeerID = string.Empty;
            IpAddress = new IPAddress(ip);
            Port = (ushort)IPAddress.NetworkToHostOrder(port);
            PeerID = string.Empty;
        }

        public Peer(string ip, string port, string peerId)
        {
            PeerID = string.Empty;
            try
            {
                IpAddress = IPAddress.Parse(ip);
                Port = (ushort)IPAddress.NetworkToHostOrder(short.Parse(port));
                PeerID = peerId;
            }
            catch
            {
            }
        }

        public override string ToString()
        {
            if (PeerID.Length > 0)
            {
                return IpAddress + ":" + Port + "(PeerID=" + PeerID + ")";
            }

            return IpAddress + ":" + Port;
        }
    }
}