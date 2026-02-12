using System;
using System.Collections.Generic;
using System.Text;

namespace TorrentClient.Application.Ports
{
    public interface IPieceStoreFactory
    {
        IPieceStore Create(string outputPath, long totalLength, long pieceLength);
    }
}
