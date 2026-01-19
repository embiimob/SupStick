using NBitcoin;
using NBitcoin.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SupStick.Services
{
    /// <summary>
    /// Bitcoin testnet3 service implementation using direct P2P connection
    /// </summary>
    public class BitcoinService : IBitcoinService
    {
        private NodesGroup? _nodesGroup;
        private readonly Network _network = Network.TestNet;
        private readonly HashSet<uint256> _seenTransactions = new();
        private readonly List<Transaction> _mempoolTransactions = new();
        private bool _isConnected;

        public BitcoinService()
        {
            InitializeP2PConnection();
        }

        private void InitializeP2PConnection()
        {
            try
            {
                // Connect directly to Bitcoin testnet3 P2P network
                var parameters = new NodeConnectionParameters();
                _nodesGroup = new NodesGroup(_network, parameters);
                
                // Subscribe to transaction events
                _nodesGroup.ConnectedNodes.Added += OnNodeAdded;
                _nodesGroup.ConnectedNodes.Removed += OnNodeRemoved;

                // Connect to testnet peers
                _nodesGroup.Connect();
                
                Console.WriteLine("Connecting to Bitcoin testnet3 P2P network...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize P2P connection: {ex.Message}");
            }
        }

        private void OnNodeAdded(object? sender, NodeEventArgs e)
        {
            Console.WriteLine($"Connected to Bitcoin node: {e.Node.RemoteSocketAddress}");
            _isConnected = true;

            // Subscribe to transaction inventory messages
            e.Node.MessageReceived += OnMessageReceived;
            
            // Request mempool transactions
            e.Node.SendMessage(new MempoolPayload());
        }

        private void OnNodeRemoved(object? sender, NodeEventArgs e)
        {
            Console.WriteLine($"Disconnected from Bitcoin node: {e.Node.RemoteSocketAddress}");
            
            if (_nodesGroup?.ConnectedNodes.Count == 0)
            {
                _isConnected = false;
            }
        }

        private void OnMessageReceived(Node node, IncomingMessage message)
        {
            try
            {
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
                        _mempoolTransactions.Add(tx);
                        
                        // Keep mempool size manageable
                        if (_mempoolTransactions.Count > 10000)
                        {
                            _mempoolTransactions.RemoveRange(0, 5000);
                        }
                        
                        Console.WriteLine($"Received transaction: {tx.GetHash()}");
                    }
                }
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
            await Task.CompletedTask;
            return _isConnected && _nodesGroup?.ConnectedNodes.Count > 0;
        }

        public async Task<List<string>> GetMempoolTransactionsAsync()
        {
            await Task.CompletedTask;
            
            lock (_mempoolTransactions)
            {
                return _mempoolTransactions
                    .Select(tx => tx.GetHash().ToString())
                    .ToList();
            }
        }

        public async Task<Transaction?> GetRawTransactionAsync(string transactionId)
        {
            await Task.CompletedTask;

            try
            {
                var txHash = uint256.Parse(transactionId);
                
                lock (_mempoolTransactions)
                {
                    var tx = _mempoolTransactions.FirstOrDefault(t => t.GetHash() == txHash);
                    if (tx != null)
                        return tx;
                }

                // Request transaction from network
                if (_nodesGroup?.ConnectedNodes.Count > 0)
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get raw transaction {transactionId}: {ex.Message}");
            }

            return null;
        }

        public async Task<Dictionary<string, object>?> GetTransactionDetailsAsync(string transactionId)
        {
            try
            {
                var tx = await GetRawTransactionAsync(transactionId);
                if (tx == null)
                    return null;

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
                    var address = output.ScriptPubKey.GetDestinationAddress(_network);
                    if (address != null)
                    {
                        scriptPubKey["addresses"] = new List<string> { address.ToString() };
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
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get transaction details for {transactionId}: {ex.Message}");
            }

            return null;
        }

        public async IAsyncEnumerable<string> MonitorNewTransactionsAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            int lastCount = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    List<string> currentTxIds;
                    
                    lock (_mempoolTransactions)
                    {
                        currentTxIds = _mempoolTransactions
                            .Skip(lastCount)
                            .Select(tx => tx.GetHash().ToString())
                            .ToList();
                        
                        lastCount = _mempoolTransactions.Count;
                    }

                    foreach (var txId in currentTxIds)
                    {
                        yield return txId;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error monitoring transactions: {ex.Message}");
                }

                // Wait before checking again
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }

        public void Dispose()
        {
            _nodesGroup?.Disconnect();
            _nodesGroup?.Dispose();
        }
    }
}
