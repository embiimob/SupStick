using SupStick.Models;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SupStick.Services
{
    /// <summary>
    /// Service for monitoring Bitcoin transactions and indexing P2FK messages
    /// </summary>
    public class TransactionMonitorService : ITransactionMonitorService
    {
        private readonly IBitcoinService _bitcoinService;
        private readonly IP2FKService _p2fkService;
        private readonly IIpfsService _ipfsService;
        private readonly IDataStorageService _dataStorage;

        private CancellationTokenSource? _monitoringCts;
        private bool _isMonitoring;

        public bool IsMonitoring => _isMonitoring;

        public event EventHandler<ItemIndexedEventArgs>? ItemIndexed;
        public event EventHandler<MonitoringStatusEventArgs>? StatusChanged;

        public TransactionMonitorService(
            IBitcoinService bitcoinService,
            IP2FKService p2fkService,
            IIpfsService ipfsService,
            IDataStorageService dataStorage)
        {
            _bitcoinService = bitcoinService;
            _p2fkService = p2fkService;
            _ipfsService = ipfsService;
            _dataStorage = dataStorage;
        }

        public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
        {
            if (_isMonitoring)
            {
                return;
            }

            _isMonitoring = true;
            _monitoringCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            OnStatusChanged(true, "Monitoring started");

            try
            {
                // Check if Bitcoin service is connected
                var isConnected = await _bitcoinService.IsConnectedAsync();
                if (!isConnected)
                {
                    OnStatusChanged(false, "Bitcoin RPC not connected");
                    _isMonitoring = false;
                    return;
                }

                // Start monitoring loop
                await foreach (var txId in _bitcoinService.MonitorNewTransactionsAsync(_monitoringCts.Token))
                {
                    try
                    {
                        await ProcessTransactionAsync(txId);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing transaction {txId}: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                OnStatusChanged(false, "Monitoring stopped");
            }
            catch (Exception ex)
            {
                OnStatusChanged(false, $"Monitoring error: {ex.Message}");
            }
            finally
            {
                _isMonitoring = false;
            }
        }

        public Task StopMonitoringAsync()
        {
            _monitoringCts?.Cancel();
            _isMonitoring = false;
            OnStatusChanged(false, "Monitoring stopped");
            return Task.CompletedTask;
        }

        private async Task ProcessTransactionAsync(string transactionId)
        {
            try
            {
                // Parse P2FK messages from the transaction
                var root = await _p2fkService.ParseTransactionAsync(transactionId);

                if (root == null)
                {
                    return; // Not a P2FK transaction
                }

                // Check if the signing address is blocked
                if (!string.IsNullOrEmpty(root.SignedBy))
                {
                    var isBlocked = await _dataStorage.IsAddressBlockedAsync(root.SignedBy);
                    if (isBlocked)
                    {
                        Console.WriteLine($"Skipping transaction from blocked address: {root.SignedBy}");
                        return;
                    }
                }

                // Process messages
                if (root.Message != null && root.Message.Length > 0)
                {
                    foreach (var message in root.Message)
                    {
                        await ProcessMessageAsync(message, root);
                    }
                }

                // Process files
                if (root.File != null && root.File.Count > 0)
                {
                    foreach (var file in root.File)
                    {
                        await ProcessFileAsync(file.Key, root);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing transaction {transactionId}: {ex.Message}");
            }
        }

        private async Task ProcessMessageAsync(string message, P2FKRoot root)
        {
            try
            {
                // Check for IPFS links in the message
                var ipfsHash = _ipfsService.ExtractIpfsHash(message);

                if (ipfsHash != null)
                {
                    // Download IPFS file
                    var fileData = await _ipfsService.DownloadFileWithRetryAsync(ipfsHash);

                    if (fileData != null)
                    {
                        // Save file locally
                        var fileName = ExtractFileNameFromMessage(message) ?? $"{ipfsHash}.bin";
                        var localPath = await SaveFileLocallyAsync(fileName, fileData);

                        // Index IPFS file
                        var item = new IndexedItem
                        {
                            TransactionId = root.TransactionId,
                            Type = "ipfs",
                            Content = message,
                            IpfsHash = ipfsHash,
                            FileName = fileName,
                            FileSize = fileData.Length,
                            SignedBy = root.SignedBy,
                            IndexedAt = DateTime.UtcNow,
                            BlockDate = root.BlockDate,
                            BlockHeight = root.BlockHeight,
                            Confirmations = root.Confirmations,
                            IsDownloaded = true,
                            LocalPath = localPath
                        };

                        await _dataStorage.SaveIndexedItemAsync(item);

                        OnItemIndexed(root.TransactionId, "ipfs", $"Downloaded: {fileName}");
                    }
                }
                else
                {
                    // Index text message
                    var item = new IndexedItem
                    {
                        TransactionId = root.TransactionId,
                        Type = "message",
                        Content = message,
                        SignedBy = root.SignedBy,
                        IndexedAt = DateTime.UtcNow,
                        BlockDate = root.BlockDate,
                        BlockHeight = root.BlockHeight,
                        Confirmations = root.Confirmations
                    };

                    await _dataStorage.SaveIndexedItemAsync(item);

                    OnItemIndexed(root.TransactionId, "message", message.Substring(0, Math.Min(50, message.Length)));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message: {ex.Message}");
            }
        }

        private async Task ProcessFileAsync(string fileName, P2FKRoot root)
        {
            try
            {
                var item = new IndexedItem
                {
                    TransactionId = root.TransactionId,
                    Type = "file",
                    FileName = fileName,
                    SignedBy = root.SignedBy,
                    IndexedAt = DateTime.UtcNow,
                    BlockDate = root.BlockDate,
                    BlockHeight = root.BlockHeight,
                    Confirmations = root.Confirmations
                };

                await _dataStorage.SaveIndexedItemAsync(item);

                OnItemIndexed(root.TransactionId, "file", fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing file {fileName}: {ex.Message}");
            }
        }

        private string? ExtractFileNameFromMessage(string message)
        {
            // Extract filename from pattern like <<IPFS:QmSdw1n...gedUg\sup.wav>>
            var match = System.Text.RegularExpressions.Regex.Match(message, @"\\([^>]+)>>");
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }

            return null;
        }

        private async Task<string> SaveFileLocallyAsync(string fileName, byte[] data)
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var filesDir = Path.Combine(appDataPath, "SupStickFiles");

            if (!Directory.Exists(filesDir))
            {
                Directory.CreateDirectory(filesDir);
            }

            // Sanitize filename
            var sanitizedFileName = SanitizeFileName(fileName);
            var filePath = Path.Combine(filesDir, sanitizedFileName);

            await File.WriteAllBytesAsync(filePath, data);

            return filePath;
        }

        private string SanitizeFileName(string fileName)
        {
            // Remove invalid characters from filename
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", fileName.Split(invalidChars));
            return sanitized;
        }

        private void OnItemIndexed(string transactionId, string type, string content)
        {
            ItemIndexed?.Invoke(this, new ItemIndexedEventArgs
            {
                TransactionId = transactionId,
                Type = type,
                Content = content
            });
        }

        private void OnStatusChanged(bool isActive, string message)
        {
            StatusChanged?.Invoke(this, new MonitoringStatusEventArgs
            {
                IsActive = isActive,
                Message = message
            });
        }
    }
}
