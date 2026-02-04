using TorrentClient.Application.Common.Interfaces;
using TorrentClient.Application.Ports;
using TorrentClient.Infrastructure.Adapters;


ILogger logger = new ConsoleLoggerAdapter();
ITorrentParser parser = new TorrentParserAdapter();


string torrentPath = args.Length > 0 ? args[0] : "test.torrent";

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

   

    Console.WriteLine("TORRENT INFORMATION                       ");
    Console.WriteLine($" Name:        {Truncate(torrent.Name, 45),-45} ");
    Console.WriteLine($" Size:        {FormatBytes(torrent.Length),-45} ");
    Console.WriteLine($" Tracker:     {Truncate(torrent.AnnounceUrl, 45),-45} ");
    Console.WriteLine($" InfoHash:    {torrent.InfoHash.ToHex(),-45} ");
    Console.WriteLine($" Pieces:      {torrent.PieceCount} x {FormatBytes(torrent.PieceLength),-40}");
    
}
catch (FileNotFoundException ex)
{
    logger.LogError($"File not found: {ex.Message}");
}
catch (InvalidDataException ex)
{
    logger.LogError($"Invalid torrent file: {ex.Message}");
}
catch (Exception ex)
{
    logger.LogError("Unexpected error", ex);
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

static string Truncate(string value, int maxChars)
{
    return value.Length <= maxChars ? value : value.Substring(0, maxChars - 3) + "...";
}