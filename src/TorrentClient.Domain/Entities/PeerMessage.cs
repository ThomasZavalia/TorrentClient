using System;
using System.Collections.Generic;
using System.Text;

namespace TorrentClient.Domain.Entities
{
    public class PeerMessage
    {
        public PeerMessageId MessageId { get; }
        public byte[] Payload { get; }

        public PeerMessage(PeerMessageId messageId, byte[]? payload = null)
        {
            MessageId = messageId;
            Payload = payload ?? Array.Empty<byte>();
        }

       
        public int TotalLength => 4 + 1 + Payload.Length;
    }
}
