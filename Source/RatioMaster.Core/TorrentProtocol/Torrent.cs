using BencodeNET.Objects;
using BencodeNET.Parsing;
using BitTorrent;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace RatioMaster.Core.TorrentProtocol
{
    public class Torrent
    {
        private Piece[] pieceArray; // an array of the torrent pieces
        private int pieces; // number of pieces the file is made up of
        private readonly SHA1 sha = new SHA1CryptoServiceProvider();

        public Collection<TorrentFile> PhysicalFiles { get; private set; }
        public long totalLength { get; private set; }
        public BDictionary Data { get; private set; }

        public Torrent()
        {
            Data = new BDictionary();
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
                return Info.ContainsKey("length");
            }
        }

        public BDictionary Info
        {
            get
            {
                return Data.Get<BDictionary>("info");
            }
        }

        public byte[] InfoHash
        {
            get
            {
                return sha.ComputeHash(Info?.EncodeAsBytes());
            }
        }

        public string Name
        {
            get
            {
                return Info.Get<BString>("name").ToString();
            }

            set
            {
                if (Data.ContainsKey("info") == false)
                {
                    Dictionary<BString, IBObject> newData = new Dictionary<BString, IBObject>();
                    newData.Add(new BString("name"), new BString(value));
                    Data.Add("info", new BDictionary(newData));
                }
                Info["name"] = new BString(value);
            }
        }

        public string Comment
        {
            get
            {
                return Data.Get<BString>("comment").ToString();
            }

            set
            {
                Data.Add("comment", value);
            }
        }

        public string Announce
        {
            get
            {
                return Data.Get<BString>("announce").ToString();
            }

            set
            {
                Data["announce"] = new BString(value);
            }
        }

        public string CreatedBy
        {
            get
            {
                return Data.Get<BString>("created by").ToString();
            }

            set
            {
                Data["created by"] = new BString(value);
            }
        }

        public bool OpenTorrent(string localFilename)
        {
            Data = null; // clear any old data
            bool hasOpened = false;
            Data = new BDictionary();
            BencodeParser bParser = new BencodeParser(Encoding.GetEncoding(1252));
            FileStream fs = null;
            BinaryReader r = null;

            try
            {
                fs = File.OpenRead(localFilename);
                r = new BinaryReader(fs);

                // Parse the BEncode .torrent file
                Data = bParser.Parse<BDictionary>(r.BaseStream);

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

            BDictionary info = Data.Get<BDictionary>("info");
            if (info.ContainsKey("pieces") == false) throw new IncompleteTorrentData("No piece hash data");

            BString pieces = info.Get<BString>("pieces");
            if ((pieces.Length % 20) != 0) throw new IncompleteTorrentData("Missing or damaged piece hash codes");

            // Parse out the hash codes
            ParsePieceHashes(pieces.EncodeAsBytes());

            // Determine what files are in the torrent
            if (SingleFile) ParseSingleFile();
            else ParseMultipleFiles();
        }

        private void ParseSingleFile()
        {
            totalLength = Info.Get<BNumber>("length").Value;
            TorrentFile f = new TorrentFile(Info.Get<BNumber>("length").Value, Info.Get<BString>("name").ToString());
            PhysicalFiles.Add(f);
        }

        private void ParseMultipleFiles()
        {
            BList files = Info.Get<BList>("files");
            PhysicalFiles = null;
            PhysicalFiles = new Collection<TorrentFile>();
            foreach (BDictionary o in files)
            {
                BList components = o.Get<BList>("path");
                bool first = true;
                string path = "";
                foreach (BString vs in components)
                {
                    if (!first) path += "/";
                    first = false;
                    path += vs.ToString();
                }

                long fileLength = o.Get<BNumber>("length").Value;
                totalLength += fileLength;
                TorrentFile f = new TorrentFile(fileLength, path);
                PhysicalFiles.Add(f);
            }
        }
    }
}
