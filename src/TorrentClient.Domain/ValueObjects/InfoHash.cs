using System;
using System.Collections.Generic;
using System.Text;

namespace TorrentClient.Domain.ValueObjects
{
    public sealed class InfoHash : IEquatable<InfoHash>
    {
        private readonly byte[] _hash;

        private InfoHash(byte[] hash)
        {
            _hash = hash;
        }

        public static InfoHash Create(byte[] hash)
        {
            if (hash == null)
                throw new ArgumentNullException(nameof(hash));
            if (hash.Length != 20)
                throw new ArgumentException("InfoHash must be exactly 20 bytes (SHA-1)", nameof(hash));

            return new InfoHash((byte[])hash.Clone()); 
        }

        public byte[] ToBytes() => (byte[])_hash.Clone();
        public string ToHex() => BitConverter.ToString(_hash).Replace("-", "").ToLower();

        public bool Equals(InfoHash? other)
        {
            if (other == null) return false;
            return _hash.SequenceEqual(other._hash);
        }

        public override bool Equals(object? obj) => Equals(obj as InfoHash);
        public override int GetHashCode() => BitConverter.ToInt32(_hash, 0); 
        public override string ToString() => ToHex();
    }
}
