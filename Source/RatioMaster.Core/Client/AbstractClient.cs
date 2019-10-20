using RatioMaster.Core.Helpers;

namespace RatioMaster.Core.Client
{
    public abstract class AbstractClient : IClient
    {
        protected static readonly RandomStringGenerator stringGenerator = new RandomStringGenerator();

        public int DefNumWant { get; set; }
        public string ProcessName { get; set; }
        public long StartOffset { get; set; }
        public long MaxOffset { get; set; }
        public string SearchString { get; set; }
        public bool Parse { get; set; }
        public bool HashUpperCase { get; set; }
        public string Headers { get; set; }
        public string HttpProtocol { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public string PeerID { get; set; }
        public string Query { get; set; }

        protected AbstractClient()
        {
            DefNumWant = 200;
            StartOffset = 10000000;
            SearchString = string.Empty;
            Parse = false;
            MaxOffset = 25000000;
            ProcessName = string.Empty;
        }

        public static IClient CreateFromName(string name)
        {
            switch(name)
            {
                case "BitComet 1.20":
                    return new BitComet();
                case "Vuze 4.2.0.8":
                    return new Vuze();
                case "Azureus 3.1.1.0":
                    return new Azureus();
                case "uTorrent 3.3.2":
                    return new uTorrent();
                case "BitTorrent 6.0.3 (8642)":
                    return new BitTorrent();
                case "Transmission 2.92 (14714)":
                    return new Transmission();
                case "Deluge 1.2.0":
                    return new Deluge();
                default:
                    return new uTorrent();
            }
        }

        protected string GenerateIdString(string keyType, int keyLength, bool urlencoding, bool upperCase = false)
        {
            string result = null;
            if (string.IsNullOrEmpty(keyType))
            {
                result = stringGenerator.Generate(keyLength);
            }
            else
            {
                if (keyType == "alphanumeric")
                {
                    result = stringGenerator.Generate(keyLength);
                }
                else if (keyType == "numeric")
                {
                    result = stringGenerator.Generate(keyLength, "0123456789".ToCharArray());
                }
                else if (keyType == "random")
                {
                    result = stringGenerator.Generate(keyLength, true);
                }
                else if (keyType == "hex")
                {
                    result = stringGenerator.Generate(keyLength, "0123456789ABCDEF".ToCharArray());
                }
            }
            if (urlencoding)
            {
                return stringGenerator.Generate(result, upperCase);
            }

            if (upperCase)
            {
                result = result.ToUpper();
            }

            return result;
        }
    }
}
