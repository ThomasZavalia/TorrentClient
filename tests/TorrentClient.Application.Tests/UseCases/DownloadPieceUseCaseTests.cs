using Moq;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using TorrentClient.Application.Common.Interfaces;
using TorrentClient.Application.Ports;
using TorrentClient.Application.UseCases;
using TorrentClient.Domain.Entities;
using TorrentClient.Domain.ValueObjects;

namespace TorrentClient.Application.Tests.UseCases
{
    public class DownloadPieceUseCaseTests
    {
        [Fact]
        public async Task ExecuteAsync_ValidPiece_ShouldReturnVerifiedData()
        {
            var loggerMock = new Mock<ILogger>();
            var connectionMock = new Mock<IPeerConnection>();

            var useCase = new DownloadPieceUseCase(loggerMock.Object);

            var pieceData = new byte[16384];
            new Random().NextBytes(pieceData);

            var hash = SHA1.Create().ComputeHash(pieceData);
            var pieces = hash; 

            var infoHash = InfoHash.Create(new byte[20]);
            var torrent = new TorrentInfo(
                "test.txt",
                16384,
                "http://tracker.test",
                infoHash,
                16384,
                pieces
            );

            connectionMock
                .Setup(c => c.SendRequestAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var payload = new byte[8 + pieceData.Length];
            BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(0, 4), 0); 
            BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(4, 4), 0); 
            pieceData.CopyTo(payload, 8);

            var pieceMsg = new PeerMessage(PeerMessageId.Piece, payload);

            connectionMock
                .Setup(c => c.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(pieceMsg);

            var result = await useCase.ExecuteAsync(
                connectionMock.Object,
                torrent,
                0,
                CancellationToken.None
            );

        
            Assert.Equal(pieceData, result);
        }

        [Fact]
        public async Task ExecuteAsync_HashMismatch_ShouldThrowException()
        {
            var loggerMock = new Mock<ILogger>();
            var connectionMock = new Mock<IPeerConnection>();

            var useCase = new DownloadPieceUseCase(loggerMock.Object);

         
            var wrongHash = new byte[20];
            new Random().NextBytes(wrongHash);

            var infoHash = InfoHash.Create(new byte[20]);
            var torrent = new TorrentInfo(
                "test.txt",
                16384,
                "http://tracker.test",
                infoHash,
                16384,
                wrongHash
            );

          
            var pieceData = new byte[16384];
            new Random().NextBytes(pieceData);

            var payload = new byte[8 + pieceData.Length];
            pieceData.CopyTo(payload, 8);

            var pieceMsg = new PeerMessage(PeerMessageId.Piece, payload);

            connectionMock
                .Setup(c => c.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(pieceMsg);

            
            await Assert.ThrowsAsync<InvalidDataException>(() =>
                useCase.ExecuteAsync(
                    connectionMock.Object,
                    torrent,
                    0,
                    CancellationToken.None
                )
            );
        }
    }
}
