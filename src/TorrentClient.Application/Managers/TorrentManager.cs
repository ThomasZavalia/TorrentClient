using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using TorrentClient.Application.Common.Interfaces;
using TorrentClient.Application.Ports;
using TorrentClient.Application.UseCases;
using TorrentClient.Domain.Entities;
using TorrentClient.Domain.ValueObjects;


namespace TorrentClient.Application.Managers
{
    public class TorrentManager
    {
        private readonly ITorrentParser _parser;
        private readonly IPeerDiscovery _peerDiscovery;
        private readonly ILogger _logger;
        private readonly DownloadPieceUseCase _downloadUseCase;
        private readonly IPeerConnectionFactory _connectionFactory; 
        private readonly IPieceStore _pieceStore; 

        private ConcurrentQueue<int> _pieceQueue;
        private TorrentInfo? _torrent;
        private PeerId? _myPeerId;
        private int _completedPieces;
        public event EventHandler<ProgressEventArgs>? ProgressChanged;
        public TorrentManager(
            ITorrentParser parser,
            IPeerDiscovery peerDiscovery,
            ILogger logger,
            DownloadPieceUseCase downloadUseCase,
            IPeerConnectionFactory connectionFactory,
            IPieceStore pieceStore)
        {
            _parser = parser;
            _peerDiscovery = peerDiscovery;
            _logger = logger;
            _downloadUseCase = downloadUseCase;
            _connectionFactory = connectionFactory;
            _pieceStore = pieceStore; 
            _pieceQueue = new ConcurrentQueue<int>();
            _completedPieces = 0;
        }

        public async Task StartAsync(string torrentPath, CancellationToken ct)
        {
            _logger.LogInfo($"Initializing download: {torrentPath}");

           
            _torrent = _parser.Parse(torrentPath);
            _myPeerId = PeerId.GenerateNew();

            _logger.LogInfo($"Torrent: {_torrent.Name}");
            _logger.LogInfo($"Size: {FormatBytes(_torrent.Length)}");
            _logger.LogInfo($"Pieces: {_torrent.PieceCount}");


            var random = new Random();
            var pieces = Enumerable.Range(0, _torrent.PieceCount)
                .OrderBy(_ => random.Next()) 
                .ToList();

            foreach (var piece in pieces)
            {
                _pieceQueue.Enqueue(piece);
            }
            _logger.LogInfo("Queue populated with random order.");

            var peers = await _peerDiscovery.DiscoverPeersAsync(
                _torrent.InfoHash,
                _torrent.AnnounceUrl,
                _myPeerId,
                _torrent.Length
            );

            var peerList = peers.ToList();
            _logger.LogInfo($"Discovered {peerList.Count} peers");

            if (peerList.Count == 0)
            {
                _logger.LogError("No peers available");
                return;
            }

            int maxConcurrency = Math.Min(peerList.Count, 5);
            _logger.LogInfo($"Starting {maxConcurrency} parallel workers");

            using var semaphore = new SemaphoreSlim(maxConcurrency);
            var tasks = new List<Task>();

            foreach (var peer in peerList)
            {
                if (_pieceQueue.IsEmpty) break;

                await semaphore.WaitAsync(ct);

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await ProcessPeerAsync(peer, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug($"Worker for {peer} failed: {ex.Message}");
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, ct));
            }

            await Task.WhenAll(tasks);

            _logger.LogInfo($"Download complete! {_completedPieces}/{_torrent.PieceCount} pieces");
        }

        private async Task ProcessPeerAsync(Peer peer, CancellationToken ct)
        {
            using var connection = _connectionFactory.Create();

            try
            {
                _logger.LogDebug($"[{peer}] Connecting...");

                using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                connectCts.CancelAfter(TimeSpan.FromSeconds(5));

                await connection.ConnectAsync(peer, connectCts.Token);

                var handshake = new Handshake(_torrent!.InfoHash, _myPeerId!);
                await connection.SendHandshakeAsync(handshake, ct);

                var response = await connection.ReceiveHandshakeAsync(ct);

                if (!response.InfoHash.Equals(_torrent.InfoHash))
                {
                    _logger.LogDebug($"[{peer}] InfoHash mismatch");
                    return;
                }

                _logger.LogDebug($"[{peer}] Handshake OK");

                var bitfieldMsg = await connection.ReceiveMessageAsync(ct);

                if (bitfieldMsg.MessageId != PeerMessageId.Bitfield)
                {
                    _logger.LogDebug($"[{peer}] Expected Bitfield, got {bitfieldMsg.MessageId}");
                    return;
                }

                var bitfield = Bitfield.FromPayload(bitfieldMsg.Payload, _torrent.PieceCount);
                _logger.LogDebug($"[{peer}] Has {bitfield.CountAvailablePieces()} pieces");

                await connection.SendMessageAsync(new PeerMessage(PeerMessageId.Interested), ct);

                var unchokeMsg = await connection.ReceiveMessageAsync(ct);

                if (unchokeMsg.MessageId != PeerMessageId.Unchoke)
                {
                    _logger.LogDebug($"[{peer}] Not unchoked (got {unchokeMsg.MessageId})");
                    return;
                }

                _logger.LogDebug($"[{peer}] Unchoked! Starting downloads");

                while (!_pieceQueue.IsEmpty)
                {
                    if (ct.IsCancellationRequested) break;

                    if (!_pieceQueue.TryDequeue(out int pieceIndex))
                    {
                        break;
                    }

                    if (!bitfield.HasPiece(pieceIndex))
                    {
                        _pieceQueue.Enqueue(pieceIndex);
                        await Task.Delay(100, ct);
                        continue;
                    }

                    try
                    {
                        _logger.LogInfo($"[{peer}] Downloading piece {pieceIndex}...");

                        var data = await _downloadUseCase.ExecuteAsync(connection, _torrent, pieceIndex, ct);
                        await _pieceStore.SavePieceAsync(pieceIndex, data, ct);

                        Interlocked.Increment(ref _completedPieces);
                        ReportProgress(); 
                    }
                    catch (Exception ex)
                    {
                      
                        _pieceQueue.Enqueue(pieceIndex);
                    }
                }            
                _logger.LogDebug($"[{peer}] Worker finished");
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug($"[{peer}] Cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"[{peer}] Error: {ex.Message}");
            }
        }

        private void ReportProgress()
        {
            if (_torrent == null) return;
            var progress = new ProgressEventArgs
            {
                CompletedPieces = _completedPieces,
                TotalPieces = _torrent.PieceCount,
                Percentage = (_completedPieces * 100.0) / _torrent.PieceCount
            };
            ProgressChanged?.Invoke(this, progress);
        }

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }


       
    }
}