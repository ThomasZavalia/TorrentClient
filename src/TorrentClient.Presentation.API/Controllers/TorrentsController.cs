using Microsoft.AspNetCore.Mvc;
using TorrentClient.Presentation.API.Services;

namespace TorrentClient.Presentation.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TorrentsController : ControllerBase
    {
        private readonly TorrentDownloadService _downloadService;
        private readonly ILogger<TorrentsController> _logger;

        public TorrentsController(
            TorrentDownloadService downloadService,
            ILogger<TorrentsController> logger)
        {
            _downloadService = downloadService;
            _logger = logger;
        }

        
        [HttpPost]
        public async Task<IActionResult> UploadTorrent(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            if (!file.FileName.EndsWith(".torrent"))
                return BadRequest("File must be a .torrent file");

            try
            {
                string uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
                Directory.CreateDirectory(uploadsDir);

                string filePath = Path.Combine(uploadsDir, $"{Guid.NewGuid()}.torrent");

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var downloadId = _downloadService.EnqueueDownload(filePath);

                _logger.LogInformation($"Torrent uploaded and queued: {downloadId}");

                return Ok(new
                {
                    downloadId,
                    message = "Download queued successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading torrent");
                return StatusCode(500, "Internal server error");
            }
        }

       
        [HttpGet("{id}")]
        public IActionResult GetDownload(Guid id)
        {
            var status = _downloadService.GetStatus(id);

            if (status == null)
                return NotFound();

            return Ok(status);
        }

     
        [HttpGet]
        public IActionResult ListDownloads()
        {
            var downloads = _downloadService.GetAllDownloads();
            return Ok(downloads);
        }
    }
}
