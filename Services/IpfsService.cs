// SupStick - Open Source Decentralized Media Player
// Copyright (c) 2026 SupStick Contributors
// Licensed under the MIT License - see LICENSE file for details
// Project: https://github.com/embiimob/SupStick

using Ipfs;
using Ipfs.CoreApi;
using Ipfs.Engine;
using Ipfs.Http;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SupStick.Services
{
    /// <summary>
    /// IPFS service implementation with direct P2P connection
    /// </summary>
    public class IpfsService : IIpfsService
    {
        // Configuration constants
        private const int EngineStartupTimeoutSeconds = 30;
        private const int BootstrapConnectionTimeoutSeconds = 30;
        private const int PeerConnectionTimeoutSeconds = 5;
        private const int FileDownloadTimeoutSeconds = 60;
        private const int ConnectionCheckTimeoutSeconds = 10;
        private const int EngineStopTimeoutMs = 5000;
        private const int RetryDelayBaseSeconds = 2;
        private const int RetryBackoffMultiplier = 2;

        private IpfsEngine? _ipfsEngine;
        private ICoreApi? _ipfs;
        private bool _isInitialized;
        private bool _isDisposed;
        private readonly SemaphoreSlim _initLock = new(1, 1);

        public IpfsService()
        {
            // Initialize asynchronously without blocking
            _ = Task.Run(async () =>
            {
                try
                {
                    await InitializeAsync();
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("IPFS initialization cancelled in background");
                }
                catch (ObjectDisposedException ex)
                {
                    Console.WriteLine($"IPFS initialization object disposed: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"IPFS initialization failed in background: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            });
        }

        private async Task InitializeAsync()
        {
            if (_isInitialized || _isDisposed)
                return;

            await _initLock.WaitAsync();
            try
            {
                if (_isInitialized || _isDisposed)
                    return;

                Console.WriteLine("Initializing IPFS engine...");

                // Create IPFS engine with local repository
                var repoPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ipfs-repo");

                Console.WriteLine($"IPFS repository path: {repoPath}");

                // Ensure repository directory exists
                if (!Directory.Exists(repoPath))
                {
                    Directory.CreateDirectory(repoPath);
                    Console.WriteLine($"Created IPFS repository directory: {repoPath}");
                }

                // Create IPFS engine instance with timeout
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(EngineStartupTimeoutSeconds));
                
                try
                {
                    _ipfsEngine = new IpfsEngine();

                    // Configure IPFS engine options with repository path
                    _ipfsEngine.Options.Repository.Folder = repoPath;

                    Console.WriteLine("Starting IPFS engine...");
                    
                    // Start the IPFS engine with cancellation token
                    await _ipfsEngine.StartAsync(timeoutCts.Token);
                    
                    _ipfs = _ipfsEngine;

                    _isInitialized = true;
                    Console.WriteLine("IPFS engine initialized successfully");

                    // Connect to bootstrap nodes (non-blocking)
                    _ = Task.Run(async () => await ConnectToBootstrapNodesAsync());
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"IPFS engine startup timed out after {EngineStartupTimeoutSeconds} seconds");
                    // Fallback to HTTP client for gateway access if direct P2P fails
                    try
                    {
                        _ipfs = new IpfsClient();
                        _isInitialized = true;
                        Console.WriteLine("Initialized IPFS HTTP client as fallback");
                    }
                    catch (Exception fallbackEx)
                    {
                        Console.WriteLine($"Failed to initialize IPFS HTTP client fallback: {fallbackEx.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("IPFS initialization was cancelled");
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine($"Object disposed during IPFS initialization: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize IPFS engine: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Fallback to HTTP client for gateway access if direct P2P fails
                try
                {
                    _ipfs = new IpfsClient();
                    _isInitialized = true;
                    Console.WriteLine("Initialized IPFS HTTP client as fallback after engine failure");
                }
                catch (Exception fallbackEx)
                {
                    Console.WriteLine($"Failed to initialize IPFS HTTP client fallback: {fallbackEx.Message}");
                }
            }
            finally
            {
                _initLock.Release();
            }
        }

        private async Task ConnectToBootstrapNodesAsync()
        {
            try
            {
                if (_ipfsEngine == null || _isDisposed)
                {
                    Console.WriteLine("IPFS engine is null or disposed, skipping bootstrap connection");
                    return;
                }

                if (_ipfs == null)
                {
                    Console.WriteLine("IPFS client is null, skipping bootstrap connection");
                    return;
                }

                Console.WriteLine("Connecting to IPFS bootstrap nodes...");

                // Get bootstrap peers and connect with timeout
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(BootstrapConnectionTimeoutSeconds));
                var bootstrapPeers = await _ipfs.Bootstrap.ListAsync(timeoutCts.Token);
                
                int connectedCount = 0;
                foreach (var peer in bootstrapPeers)
                {
                    if (_isDisposed)
                    {
                        Console.WriteLine("Service disposed during bootstrap connection");
                        return;
                    }

                    try
                    {
                        using var connectCts = new CancellationTokenSource(TimeSpan.FromSeconds(PeerConnectionTimeoutSeconds));
                        await _ipfs.Swarm.ConnectAsync(peer, connectCts.Token);
                        connectedCount++;
                        Console.WriteLine($"Connected to IPFS peer: {peer}");
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine($"Timeout connecting to peer {peer}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to connect to peer {peer}: {ex.Message}");
                    }
                }

                Console.WriteLine($"Connected to {connectedCount} IPFS bootstrap peers");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Bootstrap connection cancelled");
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine($"Object disposed during bootstrap connection: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to bootstrap nodes: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        public string GetGatewayUrl(string ipfsHash)
        {
            // Fallback gateway URL (used only if direct P2P fails)
            return $"https://ipfs.io/ipfs/{ipfsHash}";
        }

        public string? ExtractIpfsHash(string content)
        {
            if (string.IsNullOrEmpty(content))
                return null;

            // Match pattern like <<IPFS:QmSdw1n...gedUg\sup.wav>>
            var ipfsPattern = @"<<IPFS:([a-zA-Z0-9]+)(?:\\[^>]+)?>>";
            var match = Regex.Match(content, ipfsPattern);

            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }

            // Also check for standalone IPFS hashes (Qm... or baf... format)
            var hashPattern = @"\b(Qm[a-zA-Z0-9]{44,}|baf[a-zA-Z0-9]{50,})\b";
            match = Regex.Match(content, hashPattern);

            if (match.Success)
            {
                return match.Value;
            }

            return null;
        }

        public async Task<byte[]?> DownloadFileAsync(string ipfsHash)
        {
            try
            {
                if (_isDisposed)
                {
                    Console.WriteLine("IPFS service is disposed");
                    return null;
                }

                await InitializeAsync();

                if (_ipfs == null)
                {
                    Console.WriteLine("IPFS not initialized");
                    return null;
                }

                Console.WriteLine($"Downloading from IPFS P2P network: {ipfsHash}");

                // Download directly from IPFS network with timeout
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(FileDownloadTimeoutSeconds));
                
                var cid = Cid.Decode(ipfsHash);
                var stream = await _ipfs.FileSystem.ReadFileAsync(cid.Encode(), timeoutCts.Token);

                using (var memoryStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memoryStream, timeoutCts.Token);
                    var data = memoryStream.ToArray();
                    Console.WriteLine($"Successfully downloaded {data.Length} bytes from IPFS P2P");
                    return data;
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"IPFS download timeout for {ipfsHash}");
                return null;
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine($"Object disposed during IPFS download: {ex.Message}");
                return null;
            }
            catch (FormatException ex)
            {
                Console.WriteLine($"Invalid IPFS hash format {ipfsHash}: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading from IPFS P2P: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        public async Task<byte[]?> DownloadFileWithRetryAsync(string ipfsHash, int maxRetries = 3)
        {
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    if (_isDisposed)
                    {
                        Console.WriteLine("IPFS service is disposed");
                        return null;
                    }

                    Console.WriteLine($"Attempting IPFS P2P download (attempt {attempt + 1}/{maxRetries}): {ipfsHash}");

                    var data = await DownloadFileAsync(ipfsHash);
                    
                    if (data != null && data.Length > 0)
                    {
                        Console.WriteLine($"Successfully downloaded {data.Length} bytes from IPFS P2P");
                        return data;
                    }

                    Console.WriteLine($"Download attempt {attempt + 1} returned no data");
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"IPFS P2P download attempt {attempt + 1} was cancelled");
                }
                catch (ObjectDisposedException)
                {
                    Console.WriteLine($"IPFS service disposed during download attempt {attempt + 1}");
                    return null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"IPFS P2P download attempt {attempt + 1} failed: {ex.Message}");
                }

                // Wait before retrying (exponential backoff: 2^attempt * base seconds)
                if (attempt < maxRetries - 1 && !_isDisposed)
                {
                    var delay = TimeSpan.FromSeconds((1 << attempt) * RetryDelayBaseSeconds);
                    Console.WriteLine($"Waiting {delay.TotalSeconds} seconds before retry...");
                    
                    try
                    {
                        await Task.Delay(delay);

                        // Try reconnecting to peers between retries
                        if (_ipfsEngine != null && !_isDisposed)
                        {
                            await ConnectToBootstrapNodesAsync();
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        Console.WriteLine("Service disposed during retry delay");
                        return null;
                    }
                }
            }

            Console.WriteLine($"Failed to download from IPFS P2P after {maxRetries} attempts: {ipfsHash}");
            return null;
        }

        public async Task<bool> IsConnectedAsync()
        {
            try
            {
                if (_isDisposed)
                {
                    Console.WriteLine("IPFS service is disposed");
                    return false;
                }

                await InitializeAsync();

                if (_ipfs == null)
                {
                    Console.WriteLine("IPFS client is null");
                    return false;
                }

                // If we're using HTTP client fallback, consider it connected
                if (_ipfsEngine == null)
                {
                    Console.WriteLine("Using IPFS HTTP client (fallback mode)");
                    return true; // HTTP client doesn't need peer connections
                }

                // Check if we have any connected peers for P2P mode with timeout
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(ConnectionCheckTimeoutSeconds));
                var peers = await _ipfs.Swarm.PeersAsync(timeoutCts.Token);
                var peerCount = 0;
                
                foreach (var peer in peers)
                {
                    peerCount++;
                }

                Console.WriteLine($"Connected to {peerCount} IPFS peers");
                return peerCount > 0;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("IPFS connection check timed out");
                return false;
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine($"Object disposed checking IPFS connection: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking IPFS connection: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            try
            {
                if (_isDisposed)
                    return;

                _isDisposed = true;

                Console.WriteLine("Disposing IPFS service...");

                if (_ipfsEngine != null)
                {
                    try
                    {
                        var stopTask = _ipfsEngine.StopAsync();
                        if (!stopTask.Wait(EngineStopTimeoutMs))
                        {
                            Console.WriteLine($"IPFS engine stop timed out after {EngineStopTimeoutMs}ms");
                        }
                        _ipfsEngine.Dispose();
                    }
                    catch (ObjectDisposedException)
                    {
                        Console.WriteLine("IPFS engine already disposed");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error stopping IPFS engine: {ex.Message}");
                    }
                }

                _initLock?.Dispose();

                Console.WriteLine("IPFS service disposed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disposing IPFS service: {ex.Message}");
            }
        }
    }
}
