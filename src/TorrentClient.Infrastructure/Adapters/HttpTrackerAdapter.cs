using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Web;
using TorrentClient.Application.Common.Interfaces;
using TorrentClient.Application.Ports;
using TorrentClient.Domain.Entities;
using TorrentClient.Domain.ValueObjects;
using TorrentClient.Infrastructure.Adapters.Bencode;

namespace TorrentClient.Infrastructure.Adapters
{
    public class HttpTrackerAdapter : IPeerDiscovery
    {
        private readonly HttpClient _httpClient;
        private readonly BencodeDecoder _decoder;
        private readonly ILogger _logger;
        private const int ListeningPort = 6881;

        public HttpTrackerAdapter(ILogger logger)
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(15);

            _httpClient.DefaultRequestHeaders.Add("User-Agent", "qBittorrent/4.3.9");

            _decoder = new BencodeDecoder();
            _logger = logger;
        }
        public async Task<IEnumerable<Peer>> DiscoverPeersAsync(InfoHash infoHash,string announceUrl,PeerId peerId,long totalLength,long downloaded = 0,long uploaded = 0)
        {
            
            var trackerUrl = BuildTrackerUrl(
                announceUrl,
                infoHash,
                peerId,
                totalLength,
                downloaded,
                uploaded
            );

            _logger.LogInfo($"Contacting tracker: {announceUrl}");
            _logger.LogDebug($"Full URL: {trackerUrl}");

            try
            {
               
                var responseBytes = await _httpClient.GetByteArrayAsync(trackerUrl);

                try
                {
                    
                    string rawDebug = System.Text.Encoding.ASCII.GetString(responseBytes);
                    _logger.LogDebug($"RAW TRACKER RESPONSE: {rawDebug}");
                }
                catch { }


                var response = _decoder.Decode(responseBytes);

                if (response is not BencodeDictionary dict)
                    throw new InvalidDataException("Tracker response is not a dictionary");

                
                if (dict.Values.TryGetValue("failure reason", out var failureVal)
                    && failureVal is BencodeString failureStr)
                {
                    throw new InvalidOperationException($"Tracker error: {failureStr.Value}");
                }

                
                var peers = ParsePeers(dict);

                _logger.LogInfo($"Discovered {peers.Count()} peers");

                return peers;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"Failed to contact tracker: {ex.Message}", ex);
                throw;
            }
        }

        private string BuildTrackerUrl(string announceUrl,InfoHash infoHash,PeerId peerId,long totalLength,long downloaded,long uploaded)
        {
            
            var separator = announceUrl.Contains('?') ? '&' : '?';

            var parameters = new StringBuilder();
            parameters.Append($"info_hash={UrlEncodeBytes(infoHash.ToBytes())}");
            parameters.Append($"&peer_id={UrlEncodeBytes(peerId.ToBytes())}");
            parameters.Append($"&port={ListeningPort}");
            parameters.Append($"&uploaded={uploaded}");
            parameters.Append($"&downloaded={downloaded}");
            parameters.Append($"&left={totalLength - downloaded}");
            parameters.Append($"&compact=1");
            parameters.Append($"&numwant=200");
            parameters.Append($"&event=started");
            parameters.Append($"&key={GenerateKey()}");

            return $"{announceUrl}{separator}{parameters}";
        }
        private string GenerateKey()
        {
           
            return new Random().Next(100000, 999999).ToString();
        }
        private IEnumerable<Peer> ParsePeers(BencodeDictionary response)
        {
            var allPeers = new List<Peer>();

           
            if (response.Values.TryGetValue("peers", out var peersVal))
            {
                if (peersVal is BencodeString peersStr)
                    allPeers.AddRange(ParseCompactPeers(peersStr.RawBytes));
                else if (peersVal is BencodeList peersList)
                    allPeers.AddRange(ParsePeersList(peersList));
            }

           
            if (response.Values.TryGetValue("peers6", out var peers6Val) && peers6Val is BencodeString peers6Str)
            {
                allPeers.AddRange(ParseCompactPeers6(peers6Str.RawBytes));
            }

            
            if (allPeers.Count == 0 && response.Values.ContainsKey("failure reason"))
            {
                var reason = ((BencodeString)response.Values["failure reason"]).Value;
                throw new InvalidOperationException($"Tracker Failure: {reason}");
            }

            return allPeers;
        }

        private IEnumerable<Peer> ParseCompactPeers(byte[] data)
        {
            var peers = new List<Peer>();

            
            for (int i = 0; i < data.Length; i += 6)
            {
                if (i + 6 > data.Length)
                {
                    _logger.LogWarning($"Incomplete peer data at offset {i}");
                    break;
                }

               
                var ipBytes = data.AsSpan(i, 4).ToArray();
                var ip = new IPAddress(ipBytes);

                var port = (data[i + 4] << 8) | data[i + 5];

                peers.Add(new Peer(ip, port));
            }

            return peers;
        }

        private IEnumerable<Peer> ParsePeersList(BencodeList list)
        {
            var peers = new List<Peer>();

            foreach (var item in list.Values)
            {
                if (item is not BencodeDictionary peerDict)
                    continue;

                if (!peerDict.Values.TryGetValue("ip", out var ipVal) || ipVal is not BencodeString ipStr)
                    continue;

                if (!peerDict.Values.TryGetValue("port", out var portVal) || portVal is not BencodeInteger portInt)
                    continue;

                string? peerId = null;
                if (peerDict.Values.TryGetValue("peer id", out var peerIdVal) && peerIdVal is BencodeString peerIdStr)
                {
                    peerId = peerIdStr.Value;
                }

                if (IPAddress.TryParse(ipStr.Value, out var ipAddress))
                {
                    peers.Add(new Peer(ipAddress, (int)portInt.Value, peerId));
                }
            }

            return peers;
        }

        private static string UrlEncodeBytes(byte[] bytes)
        {
            var sb = new StringBuilder();
            foreach (var b in bytes)
            {
               
                if ((b >= '0' && b <= '9') ||
                    (b >= 'A' && b <= 'Z') ||
                    (b >= 'a' && b <= 'z') ||
                    b == '-' || b == '_' || b == '.' || b == '~')
                {
                    sb.Append((char)b);
                }
                else
                {
                    sb.Append($"%{b:X2}");
                }
            }
            return sb.ToString();
        }

        private IEnumerable<Peer> ParseCompactPeers6(byte[] data)
        {
            var peers = new List<Peer>();
            for (int i = 0; i < data.Length; i += 18)
            {
                if (i + 18 > data.Length) break;

                var ipBytes = data.AsSpan(i, 16).ToArray(); 
                var ip = new IPAddress(ipBytes);
                var port = (data[i + 16] << 8) | data[i + 17]; 

                peers.Add(new Peer(ip, port));
            }
            return peers;
        }
    }
}
