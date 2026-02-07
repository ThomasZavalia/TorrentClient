using System;
using System.Collections.Generic;
using System.Text;
using TorrentClient.Domain.ValueObjects;

namespace TorrentClient.Domain.Entities
{
    public class Handshake
    {
        
        public const int ProtocolStringLength = 19;
        public const string ProtocolString = "BitTorrent protocol";
        public const int HandshakeLength = 68; 

        public InfoHash InfoHash { get; }
        public PeerId PeerId { get; }
        public byte[] Reserved { get; }

        public Handshake(InfoHash infoHash, PeerId peerId, byte[]? reserved = null)
        {
            InfoHash = infoHash;
            PeerId = peerId;
            Reserved = reserved ?? new byte[8]; 

            if (Reserved.Length != 8)
                throw new ArgumentException("Reserved bytes must be length 8");
        }

        public byte[] ToBytes()
        {
            var buffer = new byte[HandshakeLength];
            var offset = 0;

           
            buffer[offset++] = (byte)ProtocolStringLength;

            
            var protocolBytes = Encoding.ASCII.GetBytes(ProtocolString);
            Array.Copy(protocolBytes, 0, buffer, offset, protocolBytes.Length);
            offset += protocolBytes.Length;

            
            Array.Copy(Reserved, 0, buffer, offset, 8);
            offset += 8;

            
            var infoHashBytes = InfoHash.ToBytes();
            Array.Copy(infoHashBytes, 0, buffer, offset, 20);
            offset += 20;

            
            var peerIdBytes = PeerId.ToBytes();
            Array.Copy(peerIdBytes, 0, buffer, offset, 20);

            return buffer;
        }


        public static Handshake FromBytes(byte[] bytes)
        {
            if (bytes.Length < HandshakeLength)
                throw new ArgumentException($"Invalid handshake length: {bytes.Length}, expected {HandshakeLength}");

            if (bytes[0] != ProtocolStringLength)
                throw new ArgumentException($"Invalid protocol length: {bytes[0]}, expected {ProtocolStringLength}");

            var protocolStr = Encoding.ASCII.GetString(bytes, 1, ProtocolStringLength);
            if (protocolStr != ProtocolString)
                throw new ArgumentException($"Unsupported protocol: {protocolStr}");

            var reserved = bytes.AsSpan(20, 8).ToArray();

            var infoHashBytes = bytes.AsSpan(28, 20).ToArray();
           
            var peerIdBytes = bytes.AsSpan(48, 20).ToArray();

            return new Handshake(
                InfoHash.Create(infoHashBytes),
                PeerId.FromBytes(peerIdBytes), 
                reserved
            );
        }
    }
}
