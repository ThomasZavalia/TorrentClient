using System;
using System.Collections.Generic;
using System.Text;
using TorrentClient.Domain.ValueObjects;

namespace TorrentClient.Domain.Entities
{
    public class TorrentInfo
    {
        public string Name { get; private set; }
        public long Length { get; private set; } 
        public string AnnounceUrl { get; private set; } 
        public InfoHash InfoHash { get; private set; } 
        public int PieceLength { get; private set; }
        public byte[] Pieces { get; private set; }

        public TorrentInfo(
         string name,
         long length,
         string announceUrl,
         InfoHash infoHash,
         int pieceLength,
         byte[] pieces)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty", nameof(name));

            if (length <= 0)
                throw new ArgumentException("Length must be positive", nameof(length));

            if (string.IsNullOrWhiteSpace(announceUrl))
                throw new ArgumentException("Announce URL cannot be empty", nameof(announceUrl));

            if (pieceLength <= 0)
                throw new ArgumentException("Piece length must be positive", nameof(pieceLength));

            if (pieces == null || pieces.Length == 0)
                throw new ArgumentException("Pieces cannot be empty", nameof(pieces));

            if (pieces.Length % 20 != 0)
                throw new ArgumentException("Pieces must be multiple of 20 bytes", nameof(pieces));

            Name = name;
            Length = length;
            AnnounceUrl = announceUrl;
            InfoHash = infoHash;
            PieceLength = pieceLength;
            Pieces = (byte[])pieces.Clone();
        }

        public int PieceCount => Pieces.Length / 20;
    }
}
