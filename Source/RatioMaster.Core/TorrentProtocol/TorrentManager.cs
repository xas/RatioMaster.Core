using BencodeNET.Objects;
using NLog;
using RatioMaster.Core.Client;
using RatioMaster.Core.NetworkProtocol;
using System;
using System.IO;
using System.Text;

namespace RatioMaster.Core.TorrentProtocol
{
    public class TorrentManager
    {
        public TorrentInfo Info { get; private set; }
        public IClient Client { get; private set; }
        public TrackerResponse Tracker { get; private set; }
        public TorrentFile File { get; private set; }

        public int Seeders { get; private set; }
        public int Leechers { get; private set; }
        public bool IsScrapeUpdated { get; private set; }
        public bool HasInitialPeers { get; private set; }

        private readonly Logger log = LogManager.GetCurrentClassLogger();

        public TorrentManager()
        {
            Info = new TorrentInfo();
        }

        public void CreateTorrentClient(string name)
        {
            Client = AbstractClient.CreateFromName(name);
        }

        public void CreateTorrentFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentNullException("TorrentManager:TorrentFileName");
            }
            File = new TorrentFile(fileName);
            Info = new TorrentInfo();
            Info.Tracker = File.Announce;
            Info.TrackerUri = new Uri(File.Announce);
            Info.Hash = File.InfoHash;
            IsScrapeUpdated = false;
            HasInitialPeers = false;
        }

        public void RequestScrape(NetworkManager network)
        {
            if (IsScrapeUpdated)
            {
                return;
            }
            try
            {
                string scrapeUrl = File.GenerateScrapeUrlString(Client.HashUpperCase);
                if (string.IsNullOrEmpty(scrapeUrl))
                {
                    log.Info("This tracker doesnt seem to support scrape");
                }
                Uri url = new Uri(scrapeUrl);
                Tracker = network.MakeWebRequest(url, Client.HttpProtocol, Client.Headers);
                if (Tracker != null && Tracker.Dico != null)
                {
                    BDictionary dico = Tracker.Dico;
                    if (dico["failure reason"] != null)
                    {
                        log.Error($"Tracker Error : {dico["failure reason"]}");
                        return;
                    }
                    log.Info("---------- Scrape Info -----------");
                    BDictionary dicoFiles = dico.Get<BDictionary>("files");
                    string infoHash = Encoding.GetEncoding(0x4e4).GetString(File.InfoHashBytes);
                    BDictionary dicoHashes = dicoFiles.Get<BDictionary>(infoHash);
                    if (dicoHashes != null)
                    {
                        Seeders = -1;
                        Leechers = -1;
                        log.Info("complete: " + dicoHashes["complete"]);
                        log.Info("downloaded: " + dicoHashes["downloaded"]);
                        log.Info("incomplete: " + dicoHashes["incomplete"]);
                        int parseInt;
                        if (int.TryParse(dicoHashes["complete"].ToString(), out parseInt))
                        {
                            Seeders = parseInt;
                        }
                        if (int.TryParse(dicoHashes["incomplete"].ToString(), out parseInt))
                        {
                            Leechers = parseInt;
                        }
                        IsScrapeUpdated = true;
                    }
                    else
                    {
                        log.Info($"Scrape returned : '{dicoFiles[infoHash]}'");
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex, "RequestScrape Error");
            }
        }

        public bool SendEventToTracker(NetworkManager networkManager, string eventType, bool stopOnFail)
        {
            IsScrapeUpdated = false;
            string urlEvent = GenerateUrlString(eventType);
            Uri uriEvent = new Uri(urlEvent);
            try
            {
                Tracker = networkManager.MakeWebRequest(uriEvent, Client.HttpProtocol, Client.Headers);
                if (Tracker != null && Tracker.Dico != null)
                {
                    BDictionary dico = Tracker.Dico;
                    BString failure = dico.Get<BString>("failure reason");
                    if (failure != null)
                    {
                        log.Warn($"Tracker Error: {failure}");
                        if (stopOnFail)
                        {
                            log.Info("Stopped because of tracker error!!!");
                            return false;
                        }
                    }
                    else
                    {
                        foreach (BString key in dico.Keys)
                        {
                            if (key != "peers")
                            {
                                log.Info(key + ": " + dico[key]);
                            }
                        }
                        BNumber interval = dico.Get<BNumber>("interval");
                        if (interval != null)
                        { }
                        BNumber complete = dico.Get<BNumber>("complete");
                        BNumber incomplete = dico.Get<BNumber>("incomplete");
                        Seeders = (int)complete.Value;
                        Leechers = (int)incomplete.Value;
                    }

                    if (dico.ContainsKey("peers"))
                    {
                        HasInitialPeers = true;
                        if (dico["peers"] is BString)
                        {
                            Encoding encoding = Encoding.GetEncoding(0x6faf);
                            byte[] buffer = encoding.GetBytes(dico["peers"].ToString());
                            using (MemoryStream ms = new MemoryStream(buffer))
                            using (BinaryReader br = new BinaryReader(ms))
                            {
                                PeerList list = new PeerList();
                                for (int num1 = 0; num1 < buffer.Length; num1 += 6)
                                {
                                    list.Add(new Peer(br.ReadBytes(4), br.ReadInt16()));
                                }
                                br.Close();
                                log.Info($"Peers : {list}");
                            }
                        }
                        else if (dico["peers"] is BList)
                        {
                            BList bList = dico.Get<BList>("peers");
                            PeerList pList = new PeerList();
                            foreach (object objList in bList)
                            {
                                if (objList is BDictionary)
                                {
                                    BDictionary dictionary = (BDictionary)objList;
                                    pList.Add(new Peer(dictionary["ip"].ToString(), dictionary["port"].ToString(), dictionary["peer id"].ToString()));
                                }
                            }
                            log.Info($"Peers : {pList}");
                        }
                        else
                        {
                            log.Info($"Peers(x) : {dico["peers"]}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
                return false;
            }
            return true;
        }

        private string GenerateUrlString(string eventType)
        {
            if (Info.Uploaded > 0)
            {
                Info.Uploaded = RoundByDenominator(Info.Uploaded, 0x4000);
            }
            if (Info.Downloaded > 0)
            {
                Info.Downloaded = RoundByDenominator(Info.Downloaded, 0x10);
            }
            if (Info.Left > 0)
            {
                Info.Left = Info.Totalsize - Info.Downloaded;
            }
            StringBuilder stbQuery = new StringBuilder(Info.Tracker);
            if (Info.Tracker.Contains("?"))
            {
                stbQuery.Append('&');
            }
            else
            {
                stbQuery.Append('?');
            }

            if (eventType.Contains("started"))
            {
                stbQuery.Replace("&natmapped=1&localip={localip}", string.Empty);
            }
            if (!eventType.Contains("stopped"))
            {
                stbQuery.Replace("&trackerid=48", string.Empty);
            }
            stbQuery.Append(Client.Query);
            stbQuery.Replace("{infohash}", File.HashUrlEncode(Info.Hash, Client.HashUpperCase));
            stbQuery.Replace("{peerid}", Info.PeerID);
            stbQuery.Replace("{port}", Info.Port.ToString());
            stbQuery.Replace("{uploaded}", Info.Uploaded.ToString());
            stbQuery.Replace("{downloaded}", Info.Downloaded.ToString());
            stbQuery.Replace("{left}", Info.Left.ToString());
            stbQuery.Replace("{event}", eventType);
            if ((Info.NumberOfPeers == "0") && !eventType.ToLower().Contains("stopped"))
            {
                Info.NumberOfPeers = "200";
            }
            stbQuery.Replace("{numwant}", Info.NumberOfPeers);
            stbQuery.Replace("{key}", Info.Key);
            stbQuery.Replace("{localip}", NetworkManager.GetIp());
            return stbQuery.ToString();
        }

        private long RoundByDenominator(long value, long denominator)
        {
            return (denominator * (value / denominator));
        }
    }
}
