using Microsoft.Extensions.Logging;

namespace NetstatSshParsing.Netstat;

public class NetstatParser
{
    private const string LOGNAME = "Netstat Parser";
    private readonly ILogger<NetstatParser> logger;

    public NetstatParser(
            ILogger<NetstatParser> logger
            )
    {
        this.logger = logger;
    }


    public TcpNetstatRow? ParseLine(string line)
    {
        var splitted = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);

        if (splitted.Length < 5)
        {
            logger.LogError("{logName}: not enough parameters to parse, line: {line}", LOGNAME, line);
            return null;
        }

        var protocol = splitted[0];

        var addrLocalFull = splitted[3];
        var localSplitted = addrLocalFull.Split(":");
        var localAddress = localSplitted[0];
        string? localPort = null;
        if (localSplitted.Length > 1)
        {
            localPort = localSplitted[1];
        }

        var addrRemoteFull = splitted[4];
        var remoteSplitted = addrRemoteFull.Split(":");
        string remoteHost = remoteSplitted[0];
        string? remotePort = null;
        if (remoteSplitted.Length > 1)
        {
            remotePort = remoteSplitted[1];
        }



        //Console.WriteLine($"Parsed without spaces: \n{string.Join("\n", splitted)}");

        return new TcpNetstatRow
        {
            Protocol = protocol,
            PortOnLocal = localPort,
            LocalAddress = localAddress,
            PortOnRemote = remotePort,
            RemoteAddress = remoteHost,
        };
    }
}

public record TcpNetstatRow
{
    public required string Protocol { get; init; }

    public required string LocalAddress { get; init; }

    public required string? PortOnLocal { get; init; }

    public required string RemoteAddress { get; init; }

    public required string? PortOnRemote { get; init; }
}
