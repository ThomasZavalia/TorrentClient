using System;
using System.Collections.Generic;
using System.Text;

namespace TorrentClient.Application.Ports
{
    public interface IPieceStore : IDisposable
    {
       
        Task SavePieceAsync(int pieceIndex, byte[] data, CancellationToken cancellationToken);
    }
}
