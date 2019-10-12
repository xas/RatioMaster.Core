using System.IO;

namespace BitTorrent
{
    public class TorrentFile
    {
        private readonly FileInfo fileInfo;
        public long Length => fileInfo.Length;
        public string Path => fileInfo.FullName;
        public string Name => fileInfo.Name;

        public TorrentFile(long len, string path) // : this()
        {
            fileInfo = new FileInfo(path);
        }
    }
}
