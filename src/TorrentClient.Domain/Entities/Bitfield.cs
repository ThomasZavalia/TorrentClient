using System;
using System.Collections.Generic;
using System.Text;

namespace TorrentClient.Domain.Entities
{
    public class Bitfield
    {
        private readonly byte[] _data;
        private readonly int _pieceCount;

        public Bitfield(byte[] data, int pieceCount)
        {
            _data = data;
            _pieceCount = pieceCount;
        }

        public bool HasPiece(int pieceIndex)
        {
            if (pieceIndex < 0 || pieceIndex >= _pieceCount)
                return false;

            int byteIndex = pieceIndex / 8;
            int bitIndex = 7 - (pieceIndex % 8); 

            if (byteIndex >= _data.Length)
                return false;

            return (_data[byteIndex] & (1 << bitIndex)) != 0;
        }

        public void SetPiece(int pieceIndex)
        {
            if (pieceIndex < 0 || pieceIndex >= _pieceCount)
                return;

            int byteIndex = pieceIndex / 8;
            int bitIndex = 7 - (pieceIndex % 8);

            if (byteIndex < _data.Length)
            {
                _data[byteIndex] |= (byte)(1 << bitIndex);
            }
        }

        public int CountAvailablePieces()
        {
            int count = 0;
            for (int i = 0; i < _pieceCount; i++)
            {
                if (HasPiece(i))
                    count++;
            }
            return count;
        }

        public static Bitfield FromPayload(byte[] payload, int totalPieces)
        {
            return new Bitfield(payload, totalPieces);
        }
    }
}
