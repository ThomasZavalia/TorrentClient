using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using TorrentClient.Application.Common.Interfaces;
using TorrentClient.Application.Ports;
using TorrentClient.Domain.Entities;

namespace TorrentClient.Application.UseCases
{
    public class DownloadPieceUseCase
    {
        private const int BlockSize = 16384; 
        private readonly ILogger _logger;

        public DownloadPieceUseCase(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<byte[]> ExecuteAsync(
            IPeerConnection connection,
            TorrentInfo torrent,
            int pieceIndex,
            CancellationToken ct)
        {
            _logger.LogInfo($"Downloading piece {pieceIndex}...");

            
            long pieceLength = CalculatePieceLength(torrent, pieceIndex);
            byte[] pieceBuffer = new byte[pieceLength];
            int downloadedBytes = 0;

            
            while (downloadedBytes < pieceLength)
            {
                int blockSize = (int)Math.Min(BlockSize, pieceLength - downloadedBytes);

               
                await connection.SendRequestAsync(pieceIndex, downloadedBytes, blockSize, ct);
              //  _logger.LogDebug($"Requested block: piece={pieceIndex}, offset={downloadedBytes}, size={blockSize}");

        
                var pieceMsg = await ReceivePieceMessageAsync(connection, ct);

                var payload = pieceMsg.Payload;

                if (payload.Length < 8)
                    throw new InvalidDataException("Piece message too short");

                int receivedIndex = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(0, 4));
                int receivedBegin = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(4, 4));
                var blockData = payload.AsSpan(8).ToArray();

                if (receivedIndex != pieceIndex)
                    throw new InvalidDataException($"Received piece {receivedIndex}, expected {pieceIndex}");

                if (receivedBegin != downloadedBytes)
                    throw new InvalidDataException($"Received offset {receivedBegin}, expected {downloadedBytes}");

                Buffer.BlockCopy(blockData, 0, pieceBuffer, downloadedBytes, blockData.Length);
                downloadedBytes += blockData.Length;

               // int progress = (int)((downloadedBytes * 100) / pieceLength);
                //_logger.LogDebug($"Progress: {downloadedBytes}/{pieceLength} bytes ({progress}%)");
            }

            _logger.LogInfo($"Piece {pieceIndex} downloaded, verifying hash...");

            var expectedHash = GetPieceHash(torrent, pieceIndex);
            var actualHash = ComputeSHA1(pieceBuffer);

            if (!actualHash.SequenceEqual(expectedHash))
            {
                throw new InvalidDataException($"Hash mismatch for piece {pieceIndex}");
            }

            _logger.LogInfo($"Piece {pieceIndex} verified successfully");

            return pieceBuffer;
        }

        private async Task<PeerMessage> ReceivePieceMessageAsync(
            IPeerConnection connection,
            CancellationToken ct)
        {
            while (true)
            {
                var msg = await connection.ReceiveMessageAsync(ct);

                if (msg.MessageId == PeerMessageId.Piece)
                {
                    return msg;
                }

                _logger.LogDebug($"Received {msg.MessageId} while waiting for Piece, ignoring...");
            }
        }

        private long CalculatePieceLength(TorrentInfo torrent, int pieceIndex)
        {
            if (pieceIndex == torrent.PieceCount - 1)
            {
                long lastPieceLength = torrent.Length - ((long)pieceIndex * torrent.PieceLength);
                return lastPieceLength;
            }

            return torrent.PieceLength;
        }

        private byte[] GetPieceHash(TorrentInfo torrent, int pieceIndex)
        {
            int offset = pieceIndex * 20;
            return torrent.Pieces.AsSpan(offset, 20).ToArray();
        }

        private byte[] ComputeSHA1(byte[] data)
        {
            using var sha1 = SHA1.Create();
            return sha1.ComputeHash(data);
        }
    }
}
