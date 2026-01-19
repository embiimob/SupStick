using Ipfs;
using Ipfs.CoreApi;
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
        private IpfsEngine? _ipfsEngine;
        private ICoreApi? _ipfs;
        private bool _isInitialized;
        private readonly SemaphoreSlim _initLock = new(1, 1);

        public IpfsService()
        {
            Task.Run(async () => await InitializeAsync());
        }

        private async Task InitializeAsync()
        {
            if (_isInitialized)
                return;

            await _initLock.WaitAsync();
            try
            {
                if (_isInitialized)
                    return;

                Console.WriteLine("Initializing IPFS engine...");

                // Create IPFS engine with local repository
                var repoPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ipfs-repo");

                _ipfsEngine = new IpfsEngine(repoPath.ToCharArray());

                // Configure IPFS engine options
                var options = new IpfsEngineOptions
                {
                    Repository = new RepositoryOptions
                    {
                        Folder = repoPath
                    }
                };

                // Start the IPFS engine
                await _ipfsEngine.StartAsync();
                _ipfs = _ipfsEngine;

                _isInitialized = true;
                Console.WriteLine("IPFS engine initialized successfully");

                // Connect to bootstrap nodes
                await ConnectToBootstrapNodesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize IPFS engine: {ex.Message}");
                // Fallback to HTTP client for gateway access if direct P2P fails
                _ipfs = new Ipfs.Http.IpfsClient();
                _isInitialized = true;
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
                if (_ipfsEngine == null)
                    return;

                Console.WriteLine("Connecting to IPFS bootstrap nodes...");

                // Get bootstrap peers and connect
                var bootstrapPeers = await _ipfs!.Bootstrap.ListAsync();
                foreach (var peer in bootstrapPeers)
                {
                    try
                    {
                        await _ipfs.Swarm.ConnectAsync(peer);
                        Console.WriteLine($"Connected to IPFS peer: {peer}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to connect to peer {peer}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to bootstrap nodes: {ex.Message}");
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
                await InitializeAsync();

                if (_ipfs == null)
                {
                    Console.WriteLine("IPFS not initialized");
                    return null;
                }

                Console.WriteLine($"Downloading from IPFS P2P network: {ipfsHash}");

                // Download directly from IPFS network
                var cid = Cid.Decode(ipfsHash);
                var stream = await _ipfs.FileSystem.ReadFileAsync(cid.Encode());

                using (var memoryStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memoryStream);
                    var data = memoryStream.ToArray();
                    Console.WriteLine($"Successfully downloaded {data.Length} bytes from IPFS P2P");
                    return data;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading from IPFS P2P: {ex.Message}");
                return null;
            }
        }

        public async Task<byte[]?> DownloadFileWithRetryAsync(string ipfsHash, int maxRetries = 3)
        {
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    Console.WriteLine($"Attempting IPFS P2P download (attempt {attempt + 1}/{maxRetries}): {ipfsHash}");

                    var data = await DownloadFileAsync(ipfsHash);
                    
                    if (data != null && data.Length > 0)
                    {
                        Console.WriteLine($"Successfully downloaded {data.Length} bytes from IPFS P2P");
                        return data;
                    }

                    Console.WriteLine($"Download attempt {attempt + 1} returned no data");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"IPFS P2P download attempt {attempt + 1} failed: {ex.Message}");
                }

                // Wait before retrying (exponential backoff)
                if (attempt < maxRetries - 1)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt) * 2);
                    Console.WriteLine($"Waiting {delay.TotalSeconds} seconds before retry...");
                    await Task.Delay(delay);

                    // Try reconnecting to peers between retries
                    if (_ipfsEngine != null)
                    {
                        await ConnectToBootstrapNodesAsync();
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
                await InitializeAsync();

                if (_ipfs == null || _ipfsEngine == null)
                    return false;

                // Check if we have any connected peers
                var peers = await _ipfs.Swarm.PeersAsync();
                var peerCount = 0;
                
                await foreach (var peer in peers)
                {
                    peerCount++;
                }

                Console.WriteLine($"Connected to {peerCount} IPFS peers");
                return peerCount > 0;
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
                _ipfsEngine?.StopAsync().Wait(5000);
                _ipfsEngine?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disposing IPFS engine: {ex.Message}");
            }
        }
    }
}
