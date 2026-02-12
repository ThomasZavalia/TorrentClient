using System;
using System.Collections.Generic;
using System.Text;
using TorrentClient.Application.Common.Interfaces;
using TorrentClient.Application.Ports;
using TorrentClient.Infrastructure.Adapters;

namespace TorrentClient.Infrastructure.Factories
{
    public class PieceStoreFactory : IPieceStoreFactory
    {
        private readonly ILogger _logger;

        public PieceStoreFactory(ILogger logger)
        {
            _logger = logger;
        }

        public IPieceStore Create(string outputPath, long totalLength, long pieceLength)
        {
            return new DiskPieceStore(outputPath, totalLength, pieceLength, _logger);
        }
    }
}
