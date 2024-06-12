namespace Metu.MarketData.Client.Options;

public class ConnectionSettings
{
    public const string Kdb = "Kdb";

    public string Host { get; set; }
    public int Port { get; set; }
    public int ConnectionCount { get; set; }
    public string SymbolFile { get; set; }
    public int SubscriptionsCount { get; set; }
    public int ListenDuration { get; set; }
    public int LoginDuration { get; set; }
    public int InstanceNum { get; set; }
}