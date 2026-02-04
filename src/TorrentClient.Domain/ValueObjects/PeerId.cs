using System;
using System.Collections.Generic;
using System.Text;

namespace TorrentClient.Domain.ValueObjects
{
    public class PeerId
    {
        private const string ClientPrefix = "-TC1000-";
        public byte[] Value { get; }

        private PeerId(byte[] value)
        {
            Value = value;
        }

        public static PeerId GenerateNew()
        {
            var bytes = new byte[20];
            var prefixBytes = Encoding.ASCII.GetBytes(ClientPrefix);
            Array.Copy(prefixBytes, bytes, prefixBytes.Length);

            var randomPart = Guid.NewGuid().ToByteArray();
            Array.Copy(randomPart, 0, bytes, prefixBytes.Length, 12);

            return new PeerId(bytes);
        }

        public byte[] ToBytes() => (byte[])Value.Clone();

        public override string ToString()
        {
            
            return Encoding.ASCII.GetString(Value, 0, Math.Min(8, Value.Length)) + "...";
        }
    }
}
