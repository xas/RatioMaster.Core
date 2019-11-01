using BytesRoad.Net.Sockets;
using NLog;
using RatioMaster.Core.TorrentProtocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RatioMaster.Core.NetworkProtocol
{
    public class NetworkManager
    {
        public ProxyType Proxy { get; private set; }
        public SocketEx Socket { get; private set; }
        public Socketeer Socketeer { get; private set; }
        public string ProxyServer { get; private set; }
        public int ProxyPort { get; private set; }
        public byte[] ProxyUser { get; private set; }
        private byte[] ProxyPassword { get; set; }

        private readonly Logger log = LogManager.GetCurrentClassLogger();

        public NetworkManager(int proxy, string server, int port, byte[] user, byte[] password)
        {
            Proxy = (ProxyType)proxy;
            ProxyServer = server;
            ProxyPort = port;
            ProxyUser = user;
            ProxyPassword = password;

            Encoding enc = Encoding.ASCII;
            log.Info("PROXY INFO:");
            log.Info("proxyType = " + Proxy);
            log.Info("proxyServer = " + ProxyServer);
            log.Info("proxyPort = " + ProxyPort);
            log.Info("proxyUser = " + enc.GetString(ProxyUser));
        }

        public void CreateTcpListener(int port, string peerId, string infoHash, byte[] infoHashBytes)
        {
            if (Socketeer == null)
            {
                Socketeer = new Socketeer(port, peerId, infoHash, infoHashBytes);
            }
        }

        public static string GetIp()
        {
            foreach (var address in Dns.GetHostEntry(string.Empty).AddressList)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    return address.ToString();
                }
            }
            return "127.0.0.1";
        }

        public TrackerResponse MakeWebRequest(Uri uriQuest, string httpProtocol, string headers)
        {
            Encoding encoder = Encoding.GetEncoding(0x4e4);
            Socket = new SocketEx(Proxy, ProxyServer, ProxyPort, ProxyUser, ProxyPassword);
            Socket.SetTimeout(0x30d40);
            Socket.PreAuthenticate = false;
            log.Info($"Connecting to {uriQuest.Host}:{uriQuest.Port}");
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    Socket.Connect(uriQuest.Host, uriQuest.Port);
                    log.Info("Connected Successfully");
                    break;
                }
                catch (Exception ex)
                {
                    log.Warn(ex);
                    log.Warn("Failed connection attempt: " + i);
                }
            }
            if (!Socket.Connected)
            {
                log.Error("Unable to connect. Quitting...");
                return null;
            }
            log.Info("======== Sending Command to Tracker ========");
            string cmd = "GET " + uriQuest.PathAndQuery + " " + httpProtocol + "\r\n" + headers.Replace("{host}", uriQuest.Host) + "\r\n";
            Socket.Send(encoder.GetBytes(cmd));

            try
            {
                byte[] data = new byte[32 * 1024];
                using (MemoryStream memStream = new MemoryStream())
                {
                    int dataLen = Socket.Receive(data);
                    while (dataLen > 0)
                    {
                        memStream.Write(data, 0, dataLen);
                        dataLen = Socket.Receive(data);
                    }

                    if (memStream.Length == 0)
                    {
                        log.Info("Error : Tracker Response is empty");
                        return null;
                    }

                    TrackerResponse trackerResponse = new TrackerResponse(memStream);
                    memStream.Close();
                    Socket.Close();

                    if (trackerResponse.doRedirect)
                    {
                        return MakeWebRequest(new Uri(trackerResponse.RedirectionURL), httpProtocol, headers);
                    }

                    log.Info("======== Tracker Response ========");
                    log.Info(trackerResponse.Headers.ToString());
                    if (trackerResponse.Dico == null)
                    {
                        log.Warn("*** Failed to decode tracker response :");
                        log.Warn(trackerResponse.Body);
                    }

                    return trackerResponse;
                }
            }
            catch (Exception ex)
            {
                Socket.Close();
                log.Error(ex);
                return null;
            }
        }

        public void Close()
        {
            if (Socketeer != null)
            {
                Socketeer.Dispose();
                Socketeer = null;
            }
            if (Socket != null)
            {
                Socket.Close();
                Socket.Dispose();
                Socket = null;
            }
        }
    }
}
