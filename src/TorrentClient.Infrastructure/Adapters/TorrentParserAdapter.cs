using System;
using System.Collections.Generic;
using System.Text;
using TorrentClient.Application.Ports;
using TorrentClient.Domain.Entities;
using TorrentClient.Domain.ValueObjects;
using System.Security.Cryptography;
using TorrentClient.Infrastructure.Adapters.Bencode;

namespace TorrentClient.Infrastructure.Adapters
{
    public class TorrentParserAdapter : ITorrentParser
    {
        private readonly BencodeDecoder _decoder;
        public TorrentParserAdapter()
        {
            _decoder = new BencodeDecoder();

        }


        public TorrentInfo Parse(string filePath)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException($"Torrent file not found: {filePath}");
            var fileBytes = File.ReadAllBytes(filePath);

            var root = _decoder.Decode(fileBytes);

            if (root is not BencodeDictionary rootDict)
                throw new InvalidDataException("Invalid torrent file: Root is not a dictionary");

            if (!rootDict.Values.TryGetValue("announce", out var announceVal) || announceVal is not BencodeString announceStr)
                throw new InvalidDataException("Missing or invalid 'announce' URL");
            if (!rootDict.Values.TryGetValue("info", out var infoVal) || infoVal is not BencodeDictionary infoDict)
                throw new InvalidDataException("Missing or invalid 'info' dictionary");
            if (!infoDict.Values.TryGetValue("name", out var nameVal) || nameVal is not BencodeString nameStr)
                throw new InvalidDataException("Missing 'name' in info dictionary");

            if (!infoDict.Values.TryGetValue("piece length", out var pieceLenVal) || pieceLenVal is not BencodeInteger pieceLenInt)
                throw new InvalidDataException("Missing 'piece length'");

            if (!infoDict.Values.TryGetValue("pieces", out var piecesVal) || piecesVal is not BencodeString piecesStr)
                throw new InvalidDataException("Missing 'pieces' hash data");

           


            long totalLength = 0;
            if (infoDict.Values.TryGetValue("length", out var lengthVal) && lengthVal is BencodeInteger lengthInt)
            {
                totalLength = lengthInt.Value;
            }
            else
            {
              
                throw new InvalidDataException("Multi-file torrents not supported yet (missing 'length')");
            }

            byte[] infoBytes = ExtractRawInfoDictionary(fileBytes);
            var infoHashBytes = ComputeSHA1(infoBytes); 
            var infoHash = InfoHash.Create(infoHashBytes);


            return new TorrentInfo(
                nameStr.Value,
                lengthInt.Value,
                announceStr.Value,
                infoHash,
                (int)pieceLenInt.Value,
                piecesStr.RawBytes 
            );
        }
        private static byte[] ComputeSHA1(byte[] data)
        {
            return SHA1.HashData(data);
        }
        private byte[] ExtractRawInfoDictionary(byte[] fileBytes)
        {
            var searchPattern = System.Text.Encoding.ASCII.GetBytes("4:info");

           
            int index = fileBytes.AsSpan().IndexOf(searchPattern);

            if (index == -1) throw new InvalidDataException("Could not find 'info' key in raw bytes");

            int valueStartIndex = index + searchPattern.Length;
            return ExtractInfoWithSmartScanner(fileBytes, valueStartIndex);
        }

        private byte[] ExtractInfoWithSmartScanner(byte[] data, int start)
        {
            
            int pos = start;
            int depth = 0;

            do
            {
                byte c = data[pos];
                if (c == 'd' || c == 'l')
                {
                    depth++;
                    pos++;
                }
                else if (c == 'i')
                {
                    pos++;
                    while (data[pos] != 'e') pos++;
                    pos++; 
                }
                else if (c >= '0' && c <= '9') 
                {
                    int lenStart = pos;
                    while (data[pos] != ':') pos++;

                    string lenStr = System.Text.Encoding.ASCII.GetString(data, lenStart, pos - lenStart);
                    int len = int.Parse(lenStr);

                    pos++; 
                    pos += len; 
                }
                else if (c == 'e')
                {
                    depth--;
                    pos++;
                }
                else
                {
                    throw new InvalidDataException($"Unexpected byte at {pos}");
                }

            } while (depth > 0 && pos < data.Length);

            int length = pos - start;
            byte[] result = new byte[length];
            Array.Copy(data, start, result, 0, length);
            return result;
        }

        private int FindPattern(byte[] data, byte[] pattern)
        {
            for (int i = 0; i < data.Length - pattern.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (data[i + j] != pattern[j])
                    {
                        found = false;
                        break;
                    }
                }
                if (found) return i;
            }
            return -1;
        }
    }
}

