using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RatioMaster.Core.NetworkProtocol
{
    public class Socketeer
    {
        private const string ProtocolDefiniton = "BitTorrent protocol";
        private readonly Encoding protocolEncoding = Encoding.GetEncoding(0x6faf);
        private readonly string PeerId;
        private readonly string InfoHash;
        private readonly byte[] InfoHashBytes;

        public TcpListener Listener { get; private set; }

        public Socketeer(int port, string peerId, string infoHash, byte[] infoHashBytes)
        {
            PeerId = peerId;
            InfoHash = infoHash;
            InfoHashBytes = infoHashBytes;
            Listener = new TcpListener(IPAddress.Any, port);
            Listener.Start();
            new Task(() => AcceptTcpConnection()).Start();
        }

        public void AcceptTcpConnection()
        {
            if (Listener == null)
            {
                throw new ArgumentNullException("AcceptTcpConnection:Listener");
            }
            while (true)
            {
                try
                {
                    using (Socket socketAccepted = Listener.AcceptSocket())
                    {
                        if (socketAccepted != null && socketAccepted.Connected)
                        {
                            byte[] buffer = new byte[0x43];
                            using (NetworkStream streamNetwork = new NetworkStream(socketAccepted))
                            {
                                streamNetwork.ReadTimeout = 0x3e8;
                                try
                                {
                                    streamNetwork.Read(buffer, 0, buffer.Length);
                                }
                                catch (Exception)
                                {
                                }

                                WriteHandShakeResponse(PeerId, buffer, streamNetwork);

                                streamNetwork.Close();
                                socketAccepted.Close();
                            }
                        }
                    }
                }
                catch (SocketException ex)
                {
                    return;
                }
            }
        }

        public bool WriteHandShakeResponse(string peerId, byte[] bufferRead, NetworkStream stream)
        {
            string bufferString = protocolEncoding.GetString(bufferRead, 0, bufferRead.Length);
            if ((bufferString.IndexOf(ProtocolDefiniton) > -1) && (bufferString.IndexOf(InfoHash) > -1))
            {
                byte[] buffer = CreateHandShake(peerId);
                stream.Write(buffer, 0, buffer.Length);
            }
            return true;
        }

        private byte[] CreateHandShake(string peerId)
        {
            List<byte> handShake = new List<byte>();
            handShake.Add((byte)ProtocolDefiniton.Length);
            handShake.AddRange(protocolEncoding.GetBytes(ProtocolDefiniton));
            for (int i = 0; i < 8; i++)
            {
                handShake.Add(0);
            }
            handShake.AddRange(InfoHashBytes);
            handShake.AddRange(protocolEncoding.GetBytes(peerId));
            return handShake.ToArray();
        }

        public void Dispose()
        {
            if (Listener != null)
            {
                try
                {
                    Listener.Stop();
                }
                catch
                {
                }
            }
        }
    }
}
