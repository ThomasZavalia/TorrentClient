using System;
using System.Collections.Generic;
using System.Text;
using TorrentClient.Domain.Entities;

namespace TorrentClient.Application.Ports
{
    public interface IPeerConnection : IDisposable
    {
        Task ConnectAsync(Peer peer, CancellationToken cancellationToken);
        Task SendHandshakeAsync(Handshake handshake, CancellationToken cancellationToken);
        Task<Handshake> ReceiveHandshakeAsync(CancellationToken cancellationToken);
        bool IsConnected { get; }

        Task SendMessageAsync(PeerMessage message, CancellationToken ct);
        Task<PeerMessage> ReceiveMessageAsync(CancellationToken ct);

        Task SendRequestAsync(int index, int begin, int length, CancellationToken ct);
    }
}
