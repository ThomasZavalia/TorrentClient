using System;
using System.Collections.Generic;
using System.Text;
using TorrentClient.Application.Common.Interfaces;
using TorrentClient.Application.Ports;
using TorrentClient.Infrastructure.Adapters;

namespace TorrentClient.Infrastructure.Factories
{
    public class PeerConnectionFactory : IPeerConnectionFactory
    {
        private readonly ILogger _logger;

        public PeerConnectionFactory(ILogger logger)
        {
            _logger = logger;
        }

        public IPeerConnection Create()
        {
            return new TcpPeerConnectionAdapter(_logger);
        }
    }
}
