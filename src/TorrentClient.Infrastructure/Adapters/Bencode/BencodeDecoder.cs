using System;
using System.Collections.Generic;
using System.Text;

namespace TorrentClient.Infrastructure.Adapters.Bencode
{
    public class BencodeDecoder
    {
        private int _position;
        private byte[] _data = Array.Empty<byte>();

        public BencodeValue Decode(byte[] data)
        {
            _data = data;
            _position = 0;
            return DecodeNext();
        }

        private BencodeValue DecodeNext()
        {
            if (_position >= _data.Length)
                throw new FormatException("Unexpected end of data");

            byte current = _data[_position];

            if (current == (byte)'i') return DecodeInteger();
            if (current >= (byte)'0' && current <= (byte)'9') return DecodeString();
            if (current == (byte)'l') return DecodeList();
            if (current == (byte)'d') return DecodeDictionary();

            throw new FormatException($"Invalid bencode format at position {_position}");
        }

        private BencodeInteger DecodeInteger()
        {
            _position++; 
            int start = _position;
            while (_position < _data.Length && _data[_position] != (byte)'e')
                _position++;

            if (_position >= _data.Length) throw new FormatException("Unterminated integer");

            string numStr = Encoding.ASCII.GetString(_data, start, _position - start);
            _position++; 

            if (!long.TryParse(numStr, out long value))
                throw new FormatException($"Invalid integer: {numStr}");

            return new BencodeInteger(value);
        }

        private BencodeString DecodeString()
        {
            int start = _position;
            while (_position < _data.Length && _data[_position] != (byte)':')
                _position++;

            if (_position >= _data.Length) throw new FormatException("Unterminated string length");

            string lengthStr = Encoding.ASCII.GetString(_data, start, _position - start);
            _position++; // Skip ':'

            if (!int.TryParse(lengthStr, out int length))
                throw new FormatException($"Invalid string length: {lengthStr}");

            if (_position + length > _data.Length)
                throw new FormatException("String length exceeds data");

            byte[] rawBytes = new byte[length];
            Array.Copy(_data, _position, rawBytes, 0, length);
            _position += length;

            string value = Encoding.UTF8.GetString(rawBytes); 
            return new BencodeString(value, rawBytes);
        }

        private BencodeList DecodeList()
        {
            _position++; 
            var values = new List<BencodeValue>();
            while (_position < _data.Length && _data[_position] != (byte)'e')
                values.Add(DecodeNext());

            _position++; 
            return new BencodeList(values);
        }

        private BencodeDictionary DecodeDictionary()
        {
            _position++; 
            var dict = new Dictionary<string, BencodeValue>();
            while (_position < _data.Length && _data[_position] != (byte)'e')
            {
                var key = DecodeString();
                var value = DecodeNext();
                dict[key.Value] = value;
            }
            _position++;
            return new BencodeDictionary(dict);
        }
    }
}
