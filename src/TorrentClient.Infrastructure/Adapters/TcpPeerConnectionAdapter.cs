using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using TorrentClient.Application.Common.Interfaces;
using TorrentClient.Application.Ports;
using TorrentClient.Domain.Entities;

namespace TorrentClient.Infrastructure.Adapters
{
    public class TcpPeerConnectionAdapter : IPeerConnection
    {
        private readonly TcpClient _tcpClient;
        private NetworkStream? _stream;
        private readonly ILogger _logger;

        public TcpPeerConnectionAdapter(ILogger logger)
        {
            _tcpClient = new TcpClient();
            _logger = logger;
        }

        public bool IsConnected => _tcpClient.Connected;

        public async Task ConnectAsync(Peer peer, CancellationToken ct)
        {
            _logger.LogDebug($"Connecting to {peer}...");

            try
            {
                await _tcpClient.ConnectAsync(peer.IpAddress, peer.Port, ct);
                _stream = _tcpClient.GetStream();

                _logger.LogDebug($"Connected to {peer}");
            }
            catch (SocketException ex)
            {
                _logger.LogDebug($"Failed to connect to {peer}: {ex.Message}");
                throw new InvalidOperationException($"Cannot connect to {peer}", ex);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug($"Connection to {peer} timed out");
                throw;
            }
        }

        public async Task SendHandshakeAsync(Handshake handshake, CancellationToken ct)
        {
            if (_stream == null)
                throw new InvalidOperationException("Not connected. Call ConnectAsync first.");

            _logger.LogDebug("Sending handshake...");

            var bytes = handshake.ToBytes();
            await _stream.WriteAsync(bytes, ct);
            await _stream.FlushAsync(ct); 

            _logger.LogDebug($"Sent {bytes.Length} bytes");
        }

        public async Task<Handshake> ReceiveHandshakeAsync(CancellationToken ct)
        {
            if (_stream == null)
                throw new InvalidOperationException("Not connected. Call ConnectAsync first.");

            _logger.LogDebug("Waiting for handshake response...");

            var buffer = new byte[Handshake.HandshakeLength];
            int totalBytesRead = 0;

           
            while (totalBytesRead < Handshake.HandshakeLength)
            {
                int bytesRead = await _stream.ReadAsync(
                    buffer.AsMemory(totalBytesRead, Handshake.HandshakeLength - totalBytesRead),
                    ct
                );

                if (bytesRead == 0)
                {
                    _logger.LogWarning("Peer closed connection during handshake");
                    throw new InvalidOperationException("Connection closed by peer during handshake");
                }

                totalBytesRead += bytesRead;
                _logger.LogDebug($"Read {bytesRead} bytes (total: {totalBytesRead}/{Handshake.HandshakeLength})");
            }

            _logger.LogDebug("Handshake received, parsing...");

            return Handshake.FromBytes(buffer);
        }

        public void Dispose()
        {
            _stream?.Dispose();
            _tcpClient.Dispose();
        }

        public async Task SendMessageAsync(PeerMessage message, CancellationToken ct)
        {
            if (_stream == null)
                throw new InvalidOperationException("Not connected");

            int messageLength = 1 + message.Payload.Length;

            var buffer = new byte[4 + messageLength];

            BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(0, 4), messageLength);

            buffer[4] = (byte)message.MessageId;

            if (message.Payload.Length > 0)
            {
                message.Payload.CopyTo(buffer.AsSpan(5));
            }

            await _stream.WriteAsync(buffer, ct);
            await _stream.FlushAsync(ct);

            _logger.LogDebug($"Sent {message.MessageId} message ({buffer.Length} bytes)");
        }


        public async Task<PeerMessage> ReceiveMessageAsync(CancellationToken ct)
        {
            if (_stream == null)
                throw new InvalidOperationException("Not connected");

            while (true) 
            {
                var lengthBuffer = new byte[4];
                int totalRead = 0;

                while (totalRead < 4)
                {
                    int bytesRead = await _stream.ReadAsync(
                        lengthBuffer.AsMemory(totalRead, 4 - totalRead),
                        ct
                    );

                    if (bytesRead == 0)
                        throw new InvalidOperationException("Connection closed by peer");

                    totalRead += bytesRead;
                }

                int messageLength = BinaryPrimitives.ReadInt32BigEndian(lengthBuffer);

                if (messageLength == 0)
                {
                    _logger.LogDebug("Received Keep-Alive");
                    continue; 
                }

                if (messageLength > 1024 * 1024 * 16) 
                {
                    throw new InvalidOperationException($"Message too large: {messageLength} bytes");
                }

                var idBuffer = new byte[1];
                if (await _stream.ReadAsync(idBuffer, ct) == 0)
                    throw new InvalidOperationException("Connection closed while reading message ID");

                var messageId = (PeerMessageId)idBuffer[0];

                int payloadLength = messageLength - 1;
                var payload = new byte[payloadLength];

                if (payloadLength > 0)
                {
                    totalRead = 0;
                    while (totalRead < payloadLength)
                    {
                        int bytesRead = await _stream.ReadAsync(
                            payload.AsMemory(totalRead, payloadLength - totalRead),
                            ct
                        );

                        if (bytesRead == 0)
                            throw new InvalidOperationException("Connection closed while reading payload");

                        totalRead += bytesRead;
                    }
                }

                _logger.LogDebug($"Received {messageId} message ({payloadLength} bytes payload)");

                return new PeerMessage(messageId, payload);
            }
        }

        public async Task SendRequestAsync(int pieceIndex, int begin, int length, CancellationToken ct)
        {
            var payload = new byte[12];

            BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(0, 4), pieceIndex);
            BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(4, 4), begin);
            BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(8, 4), length);

            var message = new PeerMessage(PeerMessageId.Request, payload);
            await SendMessageAsync(message, ct);

            _logger.LogDebug($"Sent Request: piece={pieceIndex}, offset={begin}, length={length}");
        }
    }
}
