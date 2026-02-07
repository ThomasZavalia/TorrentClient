using System;
using System.Collections.Generic;
using System.Text;
using TorrentClient.Domain.Entities;
using TorrentClient.Domain.ValueObjects;

namespace TorrentClient.Domain.Tests.Entities
{
    public class HandshakeTests
    {
        [Fact]
        public void ToBytes_ShouldGenerate68Bytes()
        {
            
            var infoHash = InfoHash.Create(new byte[20]);
            var peerId = PeerId.GenerateNew();
            var handshake = new Handshake(infoHash, peerId);

            
            var bytes = handshake.ToBytes();

        
            Assert.Equal(68, bytes.Length);
        }

        [Fact]
        public void ToBytes_ShouldStartWithProtocolLength()
        {
         
            var infoHash = InfoHash.Create(new byte[20]);
            var peerId = PeerId.GenerateNew();
            var handshake = new Handshake(infoHash, peerId);

            var bytes = handshake.ToBytes();

            
            Assert.Equal(19, bytes[0]); // Protocol length
        }

        [Fact]
        public void ToBytes_ShouldContainProtocolString()
        {
        
            var infoHash = InfoHash.Create(new byte[20]);
            var peerId = PeerId.GenerateNew();
            var handshake = new Handshake(infoHash, peerId);

         
            var bytes = handshake.ToBytes();
            var protocol = Encoding.ASCII.GetString(bytes, 1, 19);

            
            Assert.Equal("BitTorrent protocol", protocol);
        }

        [Fact]
        public void FromBytes_ToBytes_ShouldRoundTrip()
        {
            
            var infoHashBytes = new byte[20];
            new Random().NextBytes(infoHashBytes);
            var infoHash = InfoHash.Create(infoHashBytes);
            var peerId = PeerId.GenerateNew();
            var original = new Handshake(infoHash, peerId);

          
            var bytes = original.ToBytes();
            var reconstructed = Handshake.FromBytes(bytes);

            
            Assert.Equal(original.InfoHash, reconstructed.InfoHash);
            Assert.Equal(original.PeerId.ToBytes(), reconstructed.PeerId.ToBytes());
        }

        [Fact]
        public void FromBytes_InvalidLength_ShouldThrowException()
        {
           
            var invalidBytes = new byte[50]; 

            
            Assert.Throws<ArgumentException>(() => Handshake.FromBytes(invalidBytes));
        }

        [Fact]
        public void FromBytes_InvalidProtocol_ShouldThrowException()
        {
           
            var bytes = new byte[68];
            bytes[0] = 19;
            Encoding.ASCII.GetBytes("Invalid Protocol!!!").CopyTo(bytes, 1);

         
            Assert.Throws<ArgumentException>(() => Handshake.FromBytes(bytes));
        }
    }
}
