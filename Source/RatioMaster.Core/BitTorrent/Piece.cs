namespace BitTorrent
{
    using System;
    using System.IO;

    public class Piece
    {
        private Torrent torrent;

        private byte[] hash;

        private int pieceNumber;

        public byte[] Bytes
        {
            get
            {
                FileStream fs = new FileStream(torrent.PhysicalFiles[0].Path, FileMode.Open);
                BinaryReader r = new BinaryReader(fs);
                byte[] bytes = r.ReadBytes((int)fs.Length);
                r.Close();
                fs.Close();
                return bytes;
            }
        }

        public Torrent Torrent
        {
            get
            {
                return torrent;
            }
        }

        public byte[] Hash
        {
            get
            {
                return hash;
            }
        }

        public int PieceNumber
        {
            get
            {
                return pieceNumber;
            }
        }


        public Piece(Torrent parent, int pieceNumber)
        {
            hash = new byte[20];
            this.pieceNumber = pieceNumber;
            torrent = parent;

            Buffer.BlockCopy(((ValueString)torrent.Info["pieces"]).Bytes, pieceNumber * 20, hash, 0, 20);
        }
    }
}
