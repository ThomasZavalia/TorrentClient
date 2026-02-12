using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Threading.Channels;
using TorrentClient.Application.Managers;
using TorrentClient.Presentation.API.Hubs;
using static TorrentClient.Application.Managers.TorrentManager;

namespace TorrentClient.Presentation.API.Services
{
    public class TorrentDownloadService : BackgroundService
    {
        private readonly ILogger<TorrentDownloadService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHubContext<TorrentHub> _hubContext; 
        private readonly Channel<DownloadRequest> _channel;
        private readonly ConcurrentDictionary<Guid, DownloadStatus> _downloads;

        public TorrentDownloadService(
            ILogger<TorrentDownloadService> logger,
            IServiceProvider serviceProvider,
            IHubContext<TorrentHub> hubContext) 
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _hubContext = hubContext;
            _channel = Channel.CreateUnbounded<DownloadRequest>();
            _downloads = new ConcurrentDictionary<Guid, DownloadStatus>();
        }

        public Guid EnqueueDownload(string torrentPath)
        {
            var id = Guid.NewGuid();
            var request = new DownloadRequest(id, torrentPath);

            _downloads[id] = new DownloadStatus
            {
                Id = id,
                TorrentPath = torrentPath,
                TorrentName = "Loading...",
                State = DownloadState.Queued,
                QueuedAt = DateTime.UtcNow
            };

            _channel.Writer.TryWrite(request);

            _logger.LogInformation($"Enqueued download {id}");

            return id;
        }

        public DownloadStatus? GetStatus(Guid id)
        {
            _downloads.TryGetValue(id, out var status);
            return status;
        }

        public IEnumerable<DownloadStatus> GetAllDownloads()
        {
            return _downloads.Values;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TorrentDownloadService started");

            await foreach (var request in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                await ProcessDownloadAsync(request, stoppingToken);
            }

            _logger.LogInformation("TorrentDownloadService stopped");
        }

        private async Task ProcessDownloadAsync(DownloadRequest request, CancellationToken ct)
        {
            if (!_downloads.TryGetValue(request.Id, out var status))
                return;

            try
            {
                status.State = DownloadState.Downloading;
                status.StartedAt = DateTime.UtcNow;

                _logger.LogInformation($"Starting download {request.Id}");

                using var scope = _serviceProvider.CreateScope();
                var manager = scope.ServiceProvider.GetRequiredService<TorrentManager>();

                string downloadsDir = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "Downloads"
                );

                Directory.CreateDirectory(downloadsDir);

                var progressHandler = new Progress<DownloadProgress>(async progress =>
                {
                    status.TorrentName = progress.TorrentName;
                    status.TotalSize = progress.TotalSize;
                    status.CompletedPieces = progress.CompletedPieces;
                    status.TotalPieces = progress.TotalPieces;
                    status.Percentage = progress.Percentage;

                    await _hubContext.Clients.All.SendAsync("ReceiveProgress", new
                    {
                        downloadId = request.Id,
                        torrentName = progress.TorrentName,
                        totalSize = progress.TotalSize,
                        completed = progress.CompletedPieces,
                        total = progress.TotalPieces,
                        percentage = progress.Percentage,
                        state = status.State.ToString()
                    }, ct);

                    _logger.LogDebug($"[{request.Id}] Progress: {progress.CompletedPieces}/{progress.TotalPieces} ({progress.Percentage:F1}%)");
                });

                await manager.StartAsync(
                    request.TorrentPath,
                    downloadsDir,
                    ct,
                    progressHandler
                );

                status.State = DownloadState.Completed;
                status.CompletedAt = DateTime.UtcNow;

                await _hubContext.Clients.All.SendAsync("DownloadCompleted", new
                {
                    downloadId = request.Id,
                    torrentName = status.TorrentName
                }, ct);

                _logger.LogInformation($"Download {request.Id} completed");
            }
            catch (OperationCanceledException)
            {
                status.State = DownloadState.Cancelled;
                _logger.LogInformation($"Download {request.Id} cancelled");
            }
            catch (Exception ex)
            {
                status.State = DownloadState.Failed;
                status.Error = ex.Message;
                _logger.LogError(ex, $"Download {request.Id} failed");

                await _hubContext.Clients.All.SendAsync("DownloadFailed", new
                {
                    downloadId = request.Id,
                    error = ex.Message
                }, ct);
            }
        }
    }

  
    public record DownloadRequest(Guid Id, string TorrentPath);

    public class DownloadStatus
    {
        public Guid Id { get; init; }
        public string TorrentPath { get; init; } = string.Empty;
        public string TorrentName { get; set; } = string.Empty;
        public long TotalSize { get; set; }
        public DownloadState State { get; set; }
        public int CompletedPieces { get; set; }
        public int TotalPieces { get; set; }
        public double Percentage { get; set; }
        public DateTime QueuedAt { get; init; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Error { get; set; }
    }

    public enum DownloadState
    {
        Queued,
        Downloading,
        Completed,
        Failed,
        Cancelled
    }
}
