namespace BitTorrent
{
    using System.IO;

    public class TorrentFile
    {
        private readonly FileInfo fileInfo;

        public TorrentFile(long len, string path) // : this()
        {
            this.fileInfo = new FileInfo(path);
        }

        public long Length => this.fileInfo.Length;

        public string Path => this.fileInfo.FullName;

        public string Name => this.fileInfo.Name;
    }
}
