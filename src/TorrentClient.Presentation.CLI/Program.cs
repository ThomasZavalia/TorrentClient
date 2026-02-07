using TorrentClient.Application.Common.Interfaces;
using TorrentClient.Application.Ports;
using TorrentClient.Application.UseCases;
using TorrentClient.Domain.Entities;
using TorrentClient.Domain.ValueObjects;
using TorrentClient.Infrastructure.Adapters;


ILogger logger = new ConsoleLoggerAdapter();
ITorrentParser parser = new TorrentParserAdapter();
IPeerDiscovery peerDiscovery = new HttpTrackerAdapter(logger);


var myPeerId = PeerId.GenerateNew();
logger.LogInfo($"Client PeerId: {myPeerId}");

string torrentPath = args.Length > 0 ? args[0] : "debian.torrent";

try
{
    if (!File.Exists(torrentPath))
    {
        logger.LogError($"File not found: {torrentPath}");
        return;
    }

    
    logger.LogInfo($"Parsing: {torrentPath}");
    var torrent = parser.Parse(torrentPath);

    PrintTorrentInfo(torrent);

    
    logger.LogInfo("Discovering peers...");

    var peers = await peerDiscovery.DiscoverPeersAsync(
        torrent.InfoHash,
        torrent.AnnounceUrl,
        myPeerId, 
        torrent.Length
    );

    logger.LogInfo($"Discovered {peers.Count()} peers");

    PrintPeers(peers);

   
    await AttemptHandshakes(torrent, peers, myPeerId, logger);
}
catch (Exception ex)
{
    logger.LogError("Unexpected error", ex);
}

static void PrintTorrentInfo(TorrentInfo torrent)
{
    Console.WriteLine("TORRENT INFORMATION           ");
    Console.WriteLine($"  Name:        {torrent.Name}");
    Console.WriteLine($"  Size:        {FormatBytes(torrent.Length)}");
    Console.WriteLine($"  Tracker:     {torrent.AnnounceUrl}");
    Console.WriteLine($"  InfoHash:    {torrent.InfoHash.ToHex()}");
    Console.WriteLine($"  Pieces:      {torrent.PieceCount} x {FormatBytes(torrent.PieceLength)}");
    
}

static void PrintPeers(IEnumerable<Peer> peers)
{
    
    Console.WriteLine(" DISCOVERED PEERS           ");
  

    int count = 0;
    foreach (var peer in peers.Take(20))
    {
        Console.WriteLine($"  {++count:D2}. {peer}");
    }

    if (peers.Count() > 20)
    {
        Console.WriteLine($"  ... and {peers.Count() - 20} more");
    }

  
}

static async Task AttemptHandshakes(
    TorrentInfo torrent,
    IEnumerable<Peer> peers,
    PeerId myPeerId,
    ILogger logger)
{
    
    Console.WriteLine("          HANDSHAKE ATTEMPTS           ");
    

    int successCount = 0;
    int attemptCount = 0;
    const int MaxAttempts = 10; 

    foreach (var peer in peers.Take(MaxAttempts))
    {
        attemptCount++;
        Console.WriteLine($"[{attemptCount}/{MaxAttempts}] Trying {peer}...");

        using var connection = new TcpPeerConnectionAdapter(logger);

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

         
            await connection.ConnectAsync(peer, cts.Token);

            
            var handshake = new Handshake(torrent.InfoHash, myPeerId);
            await connection.SendHandshakeAsync(handshake, cts.Token);

            
            var response = await connection.ReceiveHandshakeAsync(cts.Token);

     
            if (response.InfoHash.Equals(torrent.InfoHash))
            {
                successCount++;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"SUCCESS! Peer accepted connection");
                Console.WriteLine($"Peer ID: {response.PeerId}");
                Console.ResetColor();

                if (successCount >= 1)
                {
                    Console.WriteLine($"\n Handshake protocol working! Connected to {successCount} peer(s)");
                    break;
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"FAILED: InfoHash mismatch");
                Console.ResetColor();
            }
        }
        catch (OperationCanceledException)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"TIMEOUT");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"ERROR: {ex.Message}");
            Console.ResetColor();
        }

        Console.WriteLine();
    }

    
    Console.WriteLine($"Successful: {successCount}/{attemptCount}".PadRight(41) + "");
 
}

static string FormatBytes(long bytes)
{
    string[] sizes = { "B", "KB", "MB", "GB", "TB" };
    int order = 0;
    double size = bytes;

    while (size >= 1024 && order < sizes.Length - 1)
    {
        order++;
        size /= 1024;
    }

    return $"{size:0.##} {sizes[order]}";
}