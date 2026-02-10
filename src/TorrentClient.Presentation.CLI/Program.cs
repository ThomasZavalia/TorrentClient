using TorrentClient.Application.Common.Interfaces;
using TorrentClient.Application.Managers;
using TorrentClient.Application.Ports;
using TorrentClient.Application.UseCases;
using TorrentClient.Infrastructure.Adapters;
using TorrentClient.Infrastructure.Factories;

ILogger logger = new ConsoleLoggerAdapter();
ITorrentParser parser = new TorrentParserAdapter();
IPeerDiscovery peerDiscovery = new HttpTrackerAdapter(logger);

var downloadUseCase = new DownloadPieceUseCase(logger);

IPeerConnectionFactory connectionFactory = new PeerConnectionFactory(logger);

string torrentPath = args.Length > 0 ? args[0] : "debian.torrent";

if (!File.Exists(torrentPath))
{
    logger.LogError($"File not found: {torrentPath}");
    return;
}

logger.LogInfo("Parsing torrent...");
var torrent = parser.Parse(torrentPath);

string outputPath = Path.Combine(
    Directory.GetCurrentDirectory(),
    torrent.Name
);

IPieceStore pieceStore = new DiskPieceStore(
    outputPath,
    torrent.Length,
    torrent.PieceLength,
    logger
);

var manager = new TorrentManager(
    parser,
    peerDiscovery,
    logger,
    downloadUseCase,
    connectionFactory,
    pieceStore
);

manager.ProgressChanged += (sender, args) =>
{
   
    var originalColor = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write($"\rProgress: {args.CompletedPieces}/{args.TotalPieces} pieces ({args.Percentage:F2}%)   ");
    Console.ForegroundColor = originalColor;
};

try
{
    logger.LogInfo("Starting download...");

    using var cts = new CancellationTokenSource();

    Console.CancelKeyPress += (s, e) =>
    {
        e.Cancel = true;
        logger.LogWarning("Cancellation requested...");
        cts.Cancel();
    };

  
    await manager.StartAsync(torrentPath, cts.Token);

    logger.LogInfo("Download finished successfully!");
}
catch (OperationCanceledException)
{
    logger.LogWarning("Download cancelled by user");
}
catch (Exception ex)
{
    logger.LogError("Fatal error", ex);
}
finally
{
    
    pieceStore.Dispose();
}