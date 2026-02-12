using TorrentClient.Application.Managers;

namespace TorrentClient.Presentation.API.Services
{
   /* public class TorrentBackgroundService : BackgroundService
    {
        private readonly TorrentManager _manager;
        private readonly ILogger<TorrentBackgroundService> _logger;
        private string? _torrentPath; 

        public TorrentBackgroundService(TorrentManager manager, ILogger<TorrentBackgroundService> logger)
        {
            _manager = manager;
            _logger = logger;
        }

        public void StartDownload(string path)
        {
            _torrentPath = path;
            _ = ExecuteAsync(CancellationToken.None);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (string.IsNullOrEmpty(_torrentPath)) return;

            try
            {
                await _manager.StartAsync(_torrentPath, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background torrent download");
            }
        }
    }*/
}
