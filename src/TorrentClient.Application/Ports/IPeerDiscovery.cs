using System;
using System.Collections.Generic;
using System.Text;
using TorrentClient.Domain.Entities;
using TorrentClient.Domain.ValueObjects;

namespace TorrentClient.Application.Ports
{
    public interface IPeerDiscovery
    {
        Task<IEnumerable<Peer>> DiscoverPeersAsync(InfoHash infoHash,string announceUrl,PeerId peerId,long totalLength,long downloaded = 0,long uploaded = 0
        );
    }
}
