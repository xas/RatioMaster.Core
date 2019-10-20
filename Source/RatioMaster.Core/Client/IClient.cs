namespace RatioMaster.Core.Client
{
    public interface IClient
    {
        int DefNumWant { get; set; }
        string ProcessName { get; set; }
        long StartOffset { get; set; }
        long MaxOffset { get; set; }
        string SearchString { get; set; }
        bool Parse { get; set; }
        bool HashUpperCase { get; set; }
        string Headers { get; set; }
        string HttpProtocol { get; set; }
        string Key { get; set; }
        string Name { get; set; }
        string PeerID { get; set; }
        string Query { get; set; }
    }
}
