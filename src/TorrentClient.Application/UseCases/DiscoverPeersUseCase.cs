using System;
using System.Collections.Generic;
using System.Text;
using TorrentClient.Application.Common.Interfaces;
using TorrentClient.Application.Ports;
using TorrentClient.Domain.Entities;
using TorrentClient.Domain.ValueObjects;

namespace TorrentClient.Application.UseCases
{
    public class DiscoverPeersUseCase
    {
        private readonly IPeerDiscovery _peerDiscovery;
        private readonly ILogger _logger;

        public DiscoverPeersUseCase(IPeerDiscovery peerDiscovery, ILogger logger)
        {
            _peerDiscovery = peerDiscovery;
            _logger = logger;
        }

        public async Task<IEnumerable<Peer>> ExecuteAsync(TorrentInfo torrent)
        {
            _logger.LogInfo($"Starting peer discovery for: {torrent.Name}");

           
            var peerId = PeerId.GenerateNew();

            var peers = await _peerDiscovery.DiscoverPeersAsync(
                torrent.InfoHash,
                torrent.AnnounceUrl,
                peerId,
                torrent.Length
            );

            _logger.LogInfo($"Found {peers.Count()} peers ready to connect.");
            return peers;
        }
    }
}
