using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;

namespace BitTorrent
{
    public class Torrent
    {
        private Piece[] pieceArray; // an array of the torrent pieces
        private int pieces; // number of pieces the file is made up of
        private readonly SHA1 sha = new SHA1CryptoServiceProvider();

        public Collection<TorrentFile> PhysicalFiles { get; private set; }
        public ulong totalLength { get; private set; }
        public ValueDictionary Data { get; private set; }

        public Torrent()
        {
            Data = new ValueDictionary();
            PhysicalFiles = new Collection<TorrentFile>();
        }

        public Torrent(string localFilename)
        {
            PhysicalFiles = new Collection<TorrentFile>();
            OpenTorrent(localFilename);
        }

        public bool SingleFile
        {
            get
            {
                return (((ValueDictionary)Data["info"]).ContainsKey("length"));
            }
        }

        public ValueDictionary Info
        {
            get
            {
                return (ValueDictionary)Data["info"];
            }
        }

        public byte[] InfoHash
        {
            get
            {
                return sha.ComputeHash((Data["info"]).Encode());
            }
        }

        public string Name
        {
            get
            {
                // if (data.Contains("info") == false)
                // data.Add("info", new ValueDictionary());
                return BEncode.String(((ValueDictionary)Data["info"])["name"]);
            }

            set
            {
                if (Data.ContainsKey("info") == false) Data.Add("info", new ValueDictionary());
                ((ValueDictionary)Data["info"]).SetStringValue("name", value);
            }
        }

        public string Comment
        {
            get
            {
                return BEncode.String(Data["comment"]);
            }

            set
            {
                Data.SetStringValue("comment", value);
            }
        }

        public string Announce
        {
            get
            {
                return BEncode.String(Data["announce"]);
            }

            set
            {
                Data.SetStringValue("announce", value);
            }
        }

        public string CreatedBy
        {
            get
            {
                return BEncode.String(Data["created by"]);
            }

            set
            {
                Data.SetStringValue("created by", value);
            }
        }

        public bool OpenTorrent(string localFilename)
        {
            Data = null; // clear any old data
            bool hasOpened = false;
            Data = new ValueDictionary();
            FileStream fs = null;
            BinaryReader r = null;

            try
            {
                fs = File.OpenRead(localFilename);
                r = new BinaryReader(fs);

                // Parse the BEncode .torrent file
                Data = (ValueDictionary)BEncode.Parse(r.BaseStream);

                // Check the torrent for its form, initialize this object
                LoadTorrent();

                hasOpened = true;
                r.Close();
                fs.Close();
            }
            catch (IOException)
            {
                hasOpened = false;
            }
            finally
            {
                if (r != null) r.Close();
                if (fs != null) fs.Close();
            }

            return hasOpened;
        }
        
        private void ParsePieceHashes(byte[] hashdata)
        {
            int targetPieces = hashdata.Length / 20;
            pieces = 0; // reset! careful
            pieceArray = null;
            pieceArray = new Piece[targetPieces];
            while (pieces < targetPieces)
            {
                Piece p = new Piece(this, pieces);
                pieceArray[pieces] = p;
                pieces++;
            }
        }

        public int Pieces
        {
            get
            {
                return pieceArray.Length;
            }
        }
        
        private void LoadTorrent()
        {
            if (Data.ContainsKey("announce") == false) throw new IncompleteTorrentData("No tracker URL");
            if (Data.ContainsKey("info") == false) throw new IncompleteTorrentData("No public torrent information");

            ValueDictionary info = (ValueDictionary)Data["info"];
            if (info.ContainsKey("pieces") == false) throw new IncompleteTorrentData("No piece hash data");

            ValueString pieces = (ValueString)info["pieces"];
            if ((pieces.Length % 20) != 0) throw new IncompleteTorrentData("Missing or damaged piece hash codes");

            // Parse out the hash codes
            ParsePieceHashes(pieces.Bytes);

            // Determine what files are in the torrent
            if (SingleFile) ParseSingleFile();
            else ParseMultipleFiles();
        }

        private void ParseSingleFile()
        {
            ValueDictionary info = (ValueDictionary)Data["info"];
            totalLength = (ulong)((ValueNumber)info["length"]).Integer;
            TorrentFile f = new TorrentFile(((ValueNumber)info["length"]).Integer, ((ValueString)info["name"]).StringValue);
            PhysicalFiles.Add(f);
        }

        private void ParseMultipleFiles()
        {
            ValueDictionary info = (ValueDictionary)Data["info"];
            ValueList files = (ValueList)info["files"];
            PhysicalFiles = null;
            PhysicalFiles = new Collection<TorrentFile>();
            foreach (ValueDictionary o in files)
            {
                ValueList components = (ValueList)o["path"];
                bool first = true;
                string path = "";
                foreach (ValueString vs in components)
                {
                    if (!first) path += "/";
                    first = false;
                    path += vs.StringValue;
                }

                totalLength += (ulong)((ValueNumber)o["length"]).Integer;
                TorrentFile f = new TorrentFile(((ValueNumber)o["length"]).Integer, path);
                PhysicalFiles.Add(f);
            }
        }
    }
}
