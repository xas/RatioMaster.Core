using System.Collections.Generic;

namespace RatioMaster.Core.TorrentProtocol
{
    public class PeerList : List<Peer>
    {
        public int maxPeersToShow;

        public int peerCounter;

        public PeerList()
        {
            maxPeersToShow = 5;
        }

        public override string ToString()
        {
            string result = string.Format("({0}) ", Count);
            foreach (Peer peer in this)
            {
                if (peerCounter < maxPeersToShow)
                {
                    result = result + peer + ";";
                }

                peerCounter++;
            }

            return result;
        }
    }
}