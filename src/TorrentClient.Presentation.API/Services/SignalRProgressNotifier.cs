using Microsoft.AspNetCore.SignalR;
using TorrentClient.Application.Ports;
using TorrentClient.Presentation.API.Hubs;

namespace TorrentClient.Presentation.API.Services
{
    public class SignalRProgressNotifier : IProgressNotifier
    {
        private readonly IHubContext<TorrentHub> _hubContext;
        private readonly Guid _downloadId;

        public SignalRProgressNotifier(IHubContext<TorrentHub> hubContext, Guid downloadId)
        {
            _hubContext = hubContext;
            _downloadId = downloadId;
        }

        public async Task ReportProgressAsync(int completed, int total, double percentage)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveProgress", new
            {
                downloadId = _downloadId,
                completed,
                total,
                percentage
            });
        }

        public async Task ReportLogAsync(string message)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveLog", new
            {
                downloadId = _downloadId,
                message,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
