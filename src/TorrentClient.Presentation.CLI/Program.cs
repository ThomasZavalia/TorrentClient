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

IPieceStoreFactory pieceStoreFactory = new PieceStoreFactory(logger);

string torrentPath = args.Length > 0 ? args[0] : "debian.torrent";

if (!File.Exists(torrentPath))
{
    logger.LogError($"File not found: {torrentPath}");
    return;
}


var manager = new TorrentManager(
    parser,
    peerDiscovery,
    logger,
    downloadUseCase,
    connectionFactory,
    pieceStoreFactory,
    null 
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

  
    string outputDirectory = Directory.GetCurrentDirectory();
    await manager.StartAsync(torrentPath, outputDirectory, cts.Token,null);

    logger.LogInfo("\nDownload finished successfully!");
}
catch (OperationCanceledException)
{
    logger.LogWarning("\nDownload cancelled by user");
}
catch (Exception ex)
{
    logger.LogError("\nFatal error", ex);
}