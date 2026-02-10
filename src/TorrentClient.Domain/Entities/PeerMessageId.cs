using System;
using System.Collections.Generic;
using System.Text;

namespace TorrentClient.Domain.Entities
{
    public enum PeerMessageId : byte
    {
        Choke = 0,
        Unchoke = 1,
        Interested = 2,
        NotInterested = 3,
        Have = 4,
        Bitfield = 5,
        Request = 6,
        Piece = 7,
        Cancel = 8
    }
}
