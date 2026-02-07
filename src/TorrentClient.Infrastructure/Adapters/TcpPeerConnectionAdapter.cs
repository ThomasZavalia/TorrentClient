using System;
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

        public async Task SendBytesAsync(byte[] data, CancellationToken ct)
        {
            if (_stream == null)
                throw new InvalidOperationException("Not connected");

            await _stream.WriteAsync(data, ct);
            await _stream.FlushAsync(ct);
        }

        public async Task<byte[]> ReceiveBytesAsync(int length, CancellationToken ct)
        {
            if (_stream == null)
                throw new InvalidOperationException("Not connected");

            var buffer = new byte[length];
            int totalBytesRead = 0;

            while (totalBytesRead < length)
            {
                int bytesRead = await _stream.ReadAsync(
                    buffer.AsMemory(totalBytesRead, length - totalBytesRead),
                    ct
                );

                if (bytesRead == 0)
                    throw new InvalidOperationException("Connection closed by peer");

                totalBytesRead += bytesRead;
            }

            return buffer;
        }
    }
}
