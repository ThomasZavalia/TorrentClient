using System;
using System.Collections.Generic;
using System.Text;
using TorrentClient.Application.Common.Interfaces;
using TorrentClient.Application.Ports;

namespace TorrentClient.Infrastructure.Adapters
{
    public class DiskPieceStore : IPieceStore
    {
        private readonly FileStream _fileStream;
        private readonly long _pieceLength;
        private readonly SemaphoreSlim _writeLock;
        private readonly ILogger _logger;

        public DiskPieceStore(string outputPath, long totalLength, long pieceLength, ILogger logger)
        {
            _pieceLength = pieceLength;
            _logger = logger;
            _writeLock = new SemaphoreSlim(1, 1); 

            _fileStream = new FileStream(
                outputPath,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true 
            );

            if (_fileStream.Length != totalLength)
            {
                _logger.LogInfo($"Pre-allocating file: {totalLength / 1024 / 1024} MB");
                _fileStream.SetLength(totalLength);
            }
        }

        public async Task SavePieceAsync(int pieceIndex, byte[] data, CancellationToken ct)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Piece data cannot be empty", nameof(data));

          
            long offset = (long)pieceIndex * _pieceLength;

          
            await _writeLock.WaitAsync(ct);

            try
            {
                
                _fileStream.Seek(offset, SeekOrigin.Begin);
                await _fileStream.WriteAsync(data, ct);
                await _fileStream.FlushAsync(ct);

                _logger.LogDebug($"Saved piece {pieceIndex} ({data.Length} bytes) at offset {offset}");
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public void Dispose()
        {
            _writeLock?.Dispose();
            _fileStream?.Dispose();
        }
    }
}
