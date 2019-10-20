using BencodeNET.Parsing;
using BencodeNET.Torrents;
using NLog;
using RatioMaster.Core.Helpers;
using System;
using System.Linq;
using System.Text;

namespace RatioMaster.Core.TorrentProtocol
{
    public class TorrentFile
    {
        private readonly Torrent Torrent;
        public string Announce { get; private set; }
        public string InfoHash => Torrent.GetInfoHash();
        public byte[] InfoHashBytes => Torrent.GetInfoHashBytes();
        public long TotalSize => Torrent.TotalSize;
        public string Name => Torrent.DisplayName;

        private Logger log = LogManager.GetCurrentClassLogger();

        public TorrentFile(string fileName)
        {
            BencodeParser parser = new BencodeParser();
            Torrent = parser.Parse<Torrent>(fileName);
            if (Torrent.Trackers.Any())
            {
                Announce = Torrent.Trackers.First().FirstOrDefault();
            }
        }

        public string HashUrlEncode(string decoded, bool upperCase)
        {
            StringBuilder ret = new StringBuilder();
            RandomStringGenerator stringGen = new RandomStringGenerator();
            try
            {
                for (int i = 0; i < decoded.Length; i = i + 2)
                {
                    char tempChar;

                    // the only case in which something should not be escaped, is when it is alphanum,
                    // or it's in marks
                    // in all other cases, encode it.
                    tempChar = (char)Convert.ToUInt16(decoded.Substring(i, 2), 16);
                    ret.Append(tempChar);
                }
            }
            catch (Exception ex)
            {
                log.Warn(ex);
            }

            return stringGen.Generate(ret.ToString(), upperCase);
        }

        public string GenerateScrapeUrlString(bool hashUpperCase)
        {
            UriBuilder url = new UriBuilder(Announce);
            if (url.Uri.Segments.Last() != "announce")
            {
                return string.Empty;
            }

            string hash = HashUrlEncode(Torrent.GetInfoHash(), hashUpperCase);

            url.Path = url.Path.Replace("announce", "scrape");
            if (url.Query?.Length > 1)
            {
                url.Query = url.Query.Substring(1) + "&" + $"info_hash={hash}";
            }
            else
            {
                url.Query = $"info_hash={hash}";
            }
            return url.ToString();
        }
    }
}
