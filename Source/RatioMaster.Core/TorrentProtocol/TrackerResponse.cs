using BencodeNET.Objects;
using BencodeNET.Parsing;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace RatioMaster.Core.TorrentProtocol
{
    public class TrackerResponse
    {
        private bool chunkedEncoding;
        public bool doRedirect;
        public string RedirectionURL;
        public bool response_status_302;
        public string Body { get; private set; }
        public string Charset { get; private set; }
        public string ContentEncoding { get; private set; }
        public BDictionary Dico { get; private set; }
        public StringBuilder Headers { get; private set; }

        public TrackerResponse(MemoryStream responseStream)
        {
            Headers = new StringBuilder();
            StreamReader reader = new StreamReader(responseStream);
            responseStream.Position = 0;
            string newLine = GetNewLineStr(reader);
            string lineRead = reader.ReadLine();
            while (lineRead.Length > 0)
            {
                ParseLine(lineRead);
                Headers.Append(lineRead).Append(newLine);
                lineRead = reader.ReadLine();
            }
            Headers.Append(newLine);

            responseStream.Position = Headers.Length;
            if (response_status_302 && !string.IsNullOrEmpty(RedirectionURL))
            {
                doRedirect = true;
            }
            else
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    if (!chunkedEncoding)
                    {
                        byte[] clearBuffer = new byte[responseStream.Length - responseStream.Position];
                        responseStream.Read(clearBuffer, 0, clearBuffer.Length);
                        ms.Write(clearBuffer, 0, clearBuffer.Length);
                    }
                    else
                    {
                        string currentLine = reader.ReadLine();
                        while (string.IsNullOrEmpty(currentLine))
                        {
                            string[] splittedLine = currentLine.Split(' ');
                            int convertedInt = 0;
                            if (splittedLine.Length > 0 && int.TryParse(splittedLine[0], System.Globalization.NumberStyles.HexNumber, null, out convertedInt))
                            {
                                byte[] transferBuffer = new byte[convertedInt];
                                responseStream.Position = responseStream.Position + currentLine.Length + newLine.Length;
                                responseStream.Read(transferBuffer, 0, convertedInt);
                                ms.Write(transferBuffer, 0, convertedInt);
                                reader.ReadLine(); // Jump a line
                                currentLine = reader.ReadLine();
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    ms.Position = 0;
                    ParseBEncodeDict(ms);
                    ms.Position = 0;
                    using (StreamReader bodyStream = new StreamReader(ms))
                    {
                        Body = bodyStream.ReadToEnd();
                    }
                    ms.Close();
                    reader.Close();
                    reader.Dispose();
                }
            }
        }

        private string GetNewLineStr(StreamReader streamReader)
        {
            long initialPosition = streamReader.BaseStream.Position;
            string endLine = "\r";
            char byteReaded = (char)((ushort)streamReader.BaseStream.ReadByte());
            while (byteReaded != '\r' && byteReaded != '\n')
            {
                byteReaded = (char)((ushort)streamReader.BaseStream.ReadByte());
            }
            if ((byteReaded == '\r') && (((ushort)streamReader.BaseStream.ReadByte()) == 10))
            {
                endLine = "\r\n";
            }

            streamReader.BaseStream.Position = initialPosition;
            return endLine;
        }

        private void ParseLine(string line)
        {
            if (line.IndexOf("302 Found") > -1)
            {
                response_status_302 = true;
                return;
            }
            int positionOf = line.IndexOf("Location: ");
            if (positionOf > -1)
            {
                RedirectionURL = line.Substring(positionOf + 10);
                return;
            }
            positionOf = line.IndexOf("Content-Encoding: ");
            if (positionOf > -1)
            {
                ContentEncoding = line.Substring(positionOf + 0x12).ToLower();
                return;
            }
            if (line.IndexOf("charset=") > -1)
            {
                return;
            }
            if (line.IndexOf("Transfer-Encoding: chunked") > -1)
            {
                chunkedEncoding = true;
                return;
            }
        }

        private void ParseBEncodeDict(MemoryStream responseStream)
        {
            BencodeParser bParser = new BencodeParser(Encoding.GetEncoding(1252));
            if ((ContentEncoding == "gzip") || (ContentEncoding == "x-gzip"))
            {
                using (GZipStream stream = new GZipStream(responseStream, CompressionMode.Decompress))
                {
                    try
                    {
                        Dico = bParser.Parse<BDictionary>(stream);
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            try
            {
                Dico = bParser.Parse<BDictionary>(responseStream);
            }
            catch (Exception exception1)
            {
                Console.Write(exception1.StackTrace);
            }
        }
    }
}
