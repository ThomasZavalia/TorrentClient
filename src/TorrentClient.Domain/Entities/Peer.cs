using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace TorrentClient.Domain.Entities
{
    public class Peer
    {
        public IPAddress IpAddress { get; }
        public int Port { get; }
        public string? PeerId { get; } 

        public Peer(IPAddress ipAddress, int port, string? peerId = null)
        {
            if (port <= 0 || port > 65535)
                throw new ArgumentException("Port must be between 1 and 65535", nameof(port));

            IpAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
            Port = port;
            PeerId = peerId;
        }

       
        public static Peer FromString(string ip, int port, string? peerId = null)
        {
            if (!IPAddress.TryParse(ip, out var ipAddress))
                throw new ArgumentException($"Invalid IP address: {ip}", nameof(ip));

            return new Peer(ipAddress, port, peerId);
        }

        public override string ToString() => $"{IpAddress}:{Port}";

    
        public override bool Equals(object? obj)
        {
            if (obj is not Peer other) return false;
            return IpAddress.Equals(other.IpAddress) && Port == other.Port;
        }

        public override int GetHashCode() => HashCode.Combine(IpAddress, Port);
    }
}
