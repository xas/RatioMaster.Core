using BitTorrent;
using System;
using System.IO;

namespace RatioMaster.Core.TorrentProtocol
{
    public class Piece
    {
        public Torrent Torrent { get; private set; }
        public byte[] Hash { get; private set; }
        public int PieceNumber { get; private set; }

        public Piece(Torrent parent, int pieceNumber)
        {
            Hash = new byte[20];
            PieceNumber = pieceNumber;
            Torrent = parent;

            Buffer.BlockCopy(((ValueString)Torrent.Info["pieces"]).Bytes, pieceNumber * 20, Hash, 0, 20);
        }

        public byte[] Bytes
        {
            get
            {
                using (FileStream fs = new FileStream(Torrent.PhysicalFiles[0].Path, FileMode.Open))
                using (BinaryReader r = new BinaryReader(fs))
                {
                    byte[] bytes = r.ReadBytes((int)fs.Length);
                    r.Close();
                    fs.Close();
                    return bytes;
                }
            }
        }
    }
}
