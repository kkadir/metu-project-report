using CommandLine;

namespace Metu.MarketData.Client.Options;

public class CommandLineOptions
{
    [Option('h', "Host", HelpText = "The KDB host address to be connected", Required = false)]
    public string Host { get; set; }

    [Option('p', "Port", HelpText = "The KDB host port to be connected", Required = false)]
    public int Port { get; set; }

    [Option('f', "SymbolFile", HelpText = "The symbol file full name that contains the symbols pool", Required = false)]
    public string SymbolFile { get; set; }

    [Option('s', "SubCount", HelpText = "Total random subscriptions count", Required = false)]
    public int SubscriptionsCount { get; set; }

    [Option('c', "ConnectionCount", HelpText = "Total connection count to simulate traders", Required = false)]
    public int ConnectionCount { get; set; }

    [Option('l', "ListenDuration", HelpText = "The duration in minutes to listen data", Required = false)]
    public int ListenDuration { get; set; }

    [Option('d', "LoginDuration", HelpText = "Max duration to wait for logins to complete", Required = false)]
    public int LoginDuration { get; set; }

    [Option('i', "InstanceNum", HelpText = "The instance number to identify the running process", Required = false)]
    public int InstanceNum { get; set; }
}
