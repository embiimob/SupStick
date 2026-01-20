// SupStick - Open Source Decentralized Media Player
// Copyright (c) 2026 SupStick Contributors
// Licensed under the MIT License - see LICENSE file for details
// Project: https://github.com/embiimob/SupStick

using NBitcoin;
using NBitcoin.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace SupStick.Services
{
    /// <summary>
    /// Bitcoin testnet3 service implementation using direct P2P connection
    /// </summary>
    public class BitcoinService : IBitcoinService
    {
        // Configuration constants
        private const int MaxMempoolTransactions = 10000;
        private const int MempoolCleanupThreshold = 5000;
        private const int MaxConnectionRetries = 5;
        private const int ConnectionTimeoutSeconds = 30;
        private const int RetryBackoffBaseSeconds = 2;
        private const int RetryBackoffMultiplier = 2;

        private NodesGroup? _nodesGroup;
        private readonly Network _network = Network.TestNet;
        private readonly HashSet<uint256> _seenTransactions = new();
        private readonly List<Transaction> _mempoolTransactions = new();
        private bool _isConnected;
        private bool _isDisposed;
        private readonly SemaphoreSlim _connectionLock = new(1, 1);
        private CancellationTokenSource? _connectionCts;

        public BitcoinService()
        {
            InitializeP2PConnection();
        }

        private void InitializeP2PConnection()
        {
            try
            {
                Console.WriteLine("Initializing Bitcoin testnet3 P2P connection...");
                
                // Connect directly to Bitcoin testnet3 P2P network
                var parameters = new NodeConnectionParameters();
                
                // Add connection timeout
                parameters.ConnectCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(ConnectionTimeoutSeconds)).Token;
                
                _nodesGroup = new NodesGroup(_network, parameters);
                
                // Subscribe to transaction events
                _nodesGroup.ConnectedNodes.Added += OnNodeAdded;
                _nodesGroup.ConnectedNodes.Removed += OnNodeRemoved;

                // Create cancellation token for connection
                _connectionCts = new CancellationTokenSource();

                // Connect to testnet peers with retry logic
                _ = Task.Run(async () => await ConnectWithRetryAsync(_connectionCts.Token));
                
                Console.WriteLine("Bitcoin testnet3 P2P connection initialization started");
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Socket error initializing P2P connection: {ex.Message}");
                Console.WriteLine($"Socket error code: {ex.SocketErrorCode}");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Bitcoin connection initialization was cancelled");
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine($"Object already disposed during initialization: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize P2P connection: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private async Task ConnectWithRetryAsync(CancellationToken cancellationToken)
        {
            int attempt = 0;

            while (attempt < MaxConnectionRetries && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_isDisposed || _nodesGroup == null)
                    {
                        Console.WriteLine("Bitcoin service disposed or nodes group is null");
                        return;
                    }

                    Console.WriteLine($"Attempting to connect to Bitcoin testnet3 P2P network (attempt {attempt + 1}/{MaxConnectionRetries})...");
                    
                    // Connect to testnet peers
                    _nodesGroup.Connect();
                    
                    // Wait for at least one connection
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    
                    if (_nodesGroup.ConnectedNodes.Count > 0)
                    {
                        Console.WriteLine($"Successfully connected to {_nodesGroup.ConnectedNodes.Count} Bitcoin testnet3 peer(s)");
                        return;
                    }
                    
                    Console.WriteLine("No peers connected yet, will retry...");
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Socket error connecting to Bitcoin network (attempt {attempt + 1}): {ex.Message}");
                    Console.WriteLine($"Socket error code: {ex.SocketErrorCode}");
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Bitcoin connection attempt was cancelled");
                    return;
                }
                catch (ObjectDisposedException)
                {
                    Console.WriteLine("Bitcoin service was disposed during connection");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error connecting to Bitcoin network (attempt {attempt + 1}): {ex.Message}");
                }

                attempt++;
                
                if (attempt < MaxConnectionRetries && !cancellationToken.IsCancellationRequested)
                {
                    // Exponential backoff
                    var delay = TimeSpan.FromSeconds(Math.Pow(RetryBackoffMultiplier, attempt) * RetryBackoffBaseSeconds);
                    Console.WriteLine($"Waiting {delay.TotalSeconds} seconds before retry...");
                    
                    try
                    {
                        await Task.Delay(delay, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine("Retry delay cancelled");
                        return;
                    }
                }
            }

            if (attempt >= MaxConnectionRetries)
            {
                Console.WriteLine($"Failed to connect to Bitcoin testnet3 after {MaxConnectionRetries} attempts");
            }
        }

        private void OnNodeAdded(object? sender, NodeEventArgs e)
        {
            try
            {
                if (_isDisposed)
                {
                    Console.WriteLine("Service disposed, ignoring node added event");
                    return;
                }

                Console.WriteLine($"Connected to Bitcoin node: {e.Node.RemoteSocketAddress}");
                _isConnected = true;

                // Subscribe to transaction inventory messages
                e.Node.MessageReceived += OnMessageReceived;
                
                // Request mempool transactions
                e.Node.SendMessage(new MempoolPayload());
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Socket error in OnNodeAdded: {ex.Message}");
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine($"Object disposed in OnNodeAdded: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnNodeAdded: {ex.Message}");
            }
        }

        private void OnNodeRemoved(object? sender, NodeEventArgs e)
        {
            try
            {
                Console.WriteLine($"Disconnected from Bitcoin node: {e.Node.RemoteSocketAddress}");
                
                if (_nodesGroup?.ConnectedNodes.Count == 0)
                {
                    _isConnected = false;
                    Console.WriteLine("No Bitcoin nodes connected, attempting reconnection...");
                    
                    // Attempt reconnection if not disposed
                    if (!_isDisposed && _connectionCts != null && !_connectionCts.Token.IsCancellationRequested)
                    {
                        _ = Task.Run(async () => await ConnectWithRetryAsync(_connectionCts.Token));
                    }
                }
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine($"Object disposed in OnNodeRemoved: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnNodeRemoved: {ex.Message}");
            }
        }

        private void OnMessageReceived(Node node, IncomingMessage message)
        {
            try
            {
                if (_isDisposed)
                {
                    return;
                }

                // Handle transaction inventory messages
                if (message.Message.Payload is InvPayload invPayload)
                {
                    foreach (var inv in invPayload.Inventory)
                    {
                        if (inv.Type == InventoryType.MSG_TX)
                        {
                            // Request the full transaction
                            node.SendMessage(new GetDataPayload(inv));
                        }
                    }
                }
                // Handle transaction data messages
                else if (message.Message.Payload is TxPayload txPayload)
                {
                    var tx = txPayload.Object;
                    if (!_seenTransactions.Contains(tx.GetHash()))
                    {
                        _seenTransactions.Add(tx.GetHash());
                        
                        lock (_mempoolTransactions)
                        {
                            _mempoolTransactions.Add(tx);
                            
                            // Keep mempool size manageable
                            if (_mempoolTransactions.Count > MaxMempoolTransactions)
                            {
                                _mempoolTransactions.RemoveRange(0, MempoolCleanupThreshold);
                            }
                        }
                        
                        Console.WriteLine($"Received transaction: {tx.GetHash()}");
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Socket error processing message: {ex.Message}");
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine($"Object disposed while processing message: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message: {ex.Message}");
            }
        }

        public void Configure(string url, string username, string password)
        {
            // Not needed for direct P2P connection
            // Kept for backward compatibility but does nothing
        }

        public async Task<bool> IsConnectedAsync()
        {
            try
            {
                await Task.CompletedTask;
                
                if (_isDisposed || _nodesGroup == null)
                {
                    return false;
                }

                return _isConnected && _nodesGroup.ConnectedNodes.Count > 0;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking connection status: {ex.Message}");
                return false;
            }
        }

        public async Task<List<string>> GetMempoolTransactionsAsync()
        {
            try
            {
                await Task.CompletedTask;
                
                if (_isDisposed)
                {
                    Console.WriteLine("Service disposed, returning empty transaction list");
                    return new List<string>();
                }
                
                lock (_mempoolTransactions)
                {
                    return _mempoolTransactions
                        .Select(tx => tx.GetHash().ToString())
                        .ToList();
                }
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("Object disposed in GetMempoolTransactionsAsync");
                return new List<string>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting mempool transactions: {ex.Message}");
                return new List<string>();
            }
        }

        public async Task<Transaction?> GetRawTransactionAsync(string transactionId)
        {
            try
            {
                await Task.CompletedTask;

                if (_isDisposed || _nodesGroup == null)
                {
                    Console.WriteLine("Service disposed or nodes group is null");
                    return null;
                }

                var txHash = uint256.Parse(transactionId);
                
                lock (_mempoolTransactions)
                {
                    var tx = _mempoolTransactions.FirstOrDefault(t => t.GetHash() == txHash);
                    if (tx != null)
                        return tx;
                }

                // Request transaction from network
                if (_nodesGroup.ConnectedNodes.Count > 0)
                {
                    var node = _nodesGroup.ConnectedNodes.First();
                    node.SendMessage(new GetDataPayload(new InventoryVector(InventoryType.MSG_TX, txHash)));
                    
                    // Wait a bit for the transaction to arrive
                    await Task.Delay(2000);
                    
                    lock (_mempoolTransactions)
                    {
                        return _mempoolTransactions.FirstOrDefault(t => t.GetHash() == txHash);
                    }
                }

                Console.WriteLine($"No nodes connected to request transaction {transactionId}");
                return null;
            }
            catch (FormatException ex)
            {
                Console.WriteLine($"Invalid transaction ID format {transactionId}: {ex.Message}");
                return null;
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Socket error getting raw transaction: {ex.Message}");
                return null;
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine($"Object disposed in GetRawTransactionAsync: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get raw transaction {transactionId}: {ex.Message}");
                return null;
            }
        }

        public async Task<Dictionary<string, object>?> GetTransactionDetailsAsync(string transactionId)
        {
            try
            {
                if (_isDisposed)
                {
                    Console.WriteLine("Service disposed");
                    return null;
                }

                var tx = await GetRawTransactionAsync(transactionId);
                if (tx == null)
                {
                    Console.WriteLine($"Transaction not found: {transactionId}");
                    return null;
                }

                var details = new Dictionary<string, object>
                {
                    ["txid"] = tx.GetHash().ToString(),
                    ["size"] = tx.GetSerializedSize(),
                    ["version"] = tx.Version,
                    ["locktime"] = tx.LockTime.Value
                };

                // Build vout array
                var vouts = new List<Dictionary<string, object>>();
                for (int i = 0; i < tx.Outputs.Count; i++)
                {
                    var output = tx.Outputs[i];
                    var vout = new Dictionary<string, object>
                    {
                        ["value"] = output.Value.ToDecimal(MoneyUnit.BTC).ToString("0.00000000"),
                        ["n"] = i
                    };

                    var scriptPubKey = new Dictionary<string, object>();
                    
                    // Try to extract address
                    try
                    {
                        var address = output.ScriptPubKey.GetDestinationAddress(_network);
                        if (address != null)
                        {
                            scriptPubKey["addresses"] = new List<string> { address.ToString() };
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error extracting address from output {i}: {ex.Message}");
                    }
                    
                    vout["scriptPubKey"] = scriptPubKey;
                    vouts.Add(vout);
                }

                details["vout"] = vouts;

                // Add estimated confirmations (0 for mempool)
                details["confirmations"] = 0;
                
                // Add current timestamp as blocktime for mempool transactions
                details["blocktime"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                return details;
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine($"Object disposed in GetTransactionDetailsAsync: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get transaction details for {transactionId}: {ex.Message}");
                return null;
            }
        }

        public async IAsyncEnumerable<string> MonitorNewTransactionsAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            int lastCount = 0;

            while (!cancellationToken.IsCancellationRequested && !_isDisposed)
            {
                List<string> currentTxIds = new List<string>();
                
                try
                {
                    lock (_mempoolTransactions)
                    {
                        currentTxIds = _mempoolTransactions
                            .Skip(lastCount)
                            .Select(tx => tx.GetHash().ToString())
                            .ToList();
                        
                        lastCount = _mempoolTransactions.Count;
                    }
                }
                catch (ObjectDisposedException)
                {
                    Console.WriteLine("Service disposed during transaction monitoring");
                    yield break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error monitoring transactions: {ex.Message}");
                }

                foreach (var txId in currentTxIds)
                {
                    if (cancellationToken.IsCancellationRequested)
                        yield break;
                        
                    yield return txId;
                }

                // Wait before checking again
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Transaction monitoring cancelled");
                    yield break;
                }
            }
        }

        public void Dispose()
        {
            try
            {
                if (_isDisposed)
                    return;

                _isDisposed = true;

                Console.WriteLine("Disposing Bitcoin service...");

                // Cancel any pending connections
                _connectionCts?.Cancel();
                _connectionCts?.Dispose();

                // Disconnect from nodes
                if (_nodesGroup != null)
                {
                    try
                    {
                        _nodesGroup.Disconnect();
                        _nodesGroup.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error disconnecting nodes: {ex.Message}");
                    }
                }

                _connectionLock?.Dispose();

                Console.WriteLine("Bitcoin service disposed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disposing Bitcoin service: {ex.Message}");
            }
        }
    }
}
