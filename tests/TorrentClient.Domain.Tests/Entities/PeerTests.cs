using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using TorrentClient.Domain.Entities;

namespace TorrentClient.Domain.Tests.Entities
{
    public class PeerTests
    {
        [Fact]
        public void Constructor_ValidData_ShouldCreatePeer()
        {
         
            var ip = IPAddress.Parse("192.168.1.100");
            var port = 6881;

        
            var peer = new Peer(ip, port);

           
            Assert.Equal(ip, peer.IpAddress);
            Assert.Equal(port, peer.Port);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(65536)]
        [InlineData(100000)]
        public void Constructor_InvalidPort_ShouldThrowException(int invalidPort)
        {
            
            var ip = IPAddress.Parse("192.168.1.100");

           
            Assert.Throws<ArgumentException>(() => new Peer(ip, invalidPort));
        }

        [Fact]
        public void FromString_ValidIp_ShouldCreatePeer()
        {
            
            var peer = Peer.FromString("192.168.1.100", 6881);

            
            Assert.Equal("192.168.1.100", peer.IpAddress.ToString());
            Assert.Equal(6881, peer.Port);
        }

        [Fact]
        public void FromString_InvalidIp_ShouldThrowException()
        {
           
            Assert.Throws<ArgumentException>(() => Peer.FromString("invalid.ip", 6881));
        }

        [Fact]
        public void ToString_ShouldReturnIpAndPort()
        {
            
            var peer = Peer.FromString("192.168.1.100", 6881);

           
            var result = peer.ToString();

            
            Assert.Equal("192.168.1.100:6881", result);
        }
    }
}
