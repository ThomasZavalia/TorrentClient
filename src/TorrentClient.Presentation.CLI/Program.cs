using TorrentClient.Application.Common.Interfaces;
using TorrentClient.Application.Ports;
using TorrentClient.Application.UseCases;
using TorrentClient.Infrastructure.Adapters;


ILogger logger = new ConsoleLoggerAdapter();
ITorrentParser parser = new TorrentParserAdapter();
IPeerDiscovery peerDiscovery = new HttpTrackerAdapter(logger);

var discoverPeersUseCase = new DiscoverPeersUseCase(peerDiscovery, logger);

string torrentPath = args.Length > 0 ? args[0] : "debian.torrent";

try
{
    if (!File.Exists(torrentPath))
    {
        logger.LogError($"File not found: {torrentPath}");
        Console.WriteLine("Usage: dotnet run -- <path-to-torrent-file>");
        return;
    }

    logger.LogInfo($"Parsing: {torrentPath}...");
    var torrent = parser.Parse(torrentPath);

    Console.WriteLine($"\n[Torrent Loaded] {torrent.Name} ({torrent.Length / 1024 / 1024} MB)");
    Console.WriteLine($"Tracker: {torrent.AnnounceUrl}\n");

   
    logger.LogInfo("Contacting Tracker...");
    var peers = await discoverPeersUseCase.ExecuteAsync(torrent);

  
    Console.WriteLine("DISCOVERED PEERS         ");
   

    int count = 0;
    foreach (var peer in peers.Take(25)) 
    {
        Console.WriteLine($" {++count,2}. {peer.ToString(),-35} ");
    }

    if (peers.Count() > 25)
        Console.WriteLine($" ... and {peers.Count() - 25} more            ");

    

}
catch (Exception ex)
{
    logger.LogError("Critical Failure", ex);
}