using NBitcoin;
using NBitcoin.RPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SupStick.Services
{
    /// <summary>
    /// Bitcoin testnet3 service implementation
    /// </summary>
    public class BitcoinService : IBitcoinService
    {
        private RPCClient? _rpcClient;
        private string _rpcUrl;
        private string _rpcUsername;
        private string _rpcPassword;
        private readonly Network _network = Network.TestNet;

        public BitcoinService()
        {
            // Default testnet3 configuration - can be updated through settings
            _rpcUrl = "http://127.0.0.1:18332";
            _rpcUsername = "user";
            _rpcPassword = "pass";
        }

        public void Configure(string url, string username, string password)
        {
            _rpcUrl = url;
            _rpcUsername = username;
            _rpcPassword = password;
            InitializeClient();
        }

        private void InitializeClient()
        {
            try
            {
                var credentials = new NetworkCredential(_rpcUsername, _rpcPassword);
                _rpcClient = new RPCClient(credentials, new Uri(_rpcUrl), _network);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize Bitcoin RPC client: {ex.Message}");
            }
        }

        public async Task<bool> IsConnectedAsync()
        {
            if (_rpcClient == null)
            {
                InitializeClient();
            }

            try
            {
                if (_rpcClient != null)
                {
                    var blockCount = await _rpcClient.GetBlockCountAsync();
                    return blockCount > 0;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        public async Task<List<string>> GetMempoolTransactionsAsync()
        {
            if (_rpcClient == null)
            {
                InitializeClient();
            }

            try
            {
                if (_rpcClient != null)
                {
                    var mempoolInfo = await _rpcClient.SendCommandAsync("getrawmempool");
                    if (mempoolInfo?.Result != null)
                    {
                        var txids = mempoolInfo.Result.ToString();
                        // Parse the JSON array of transaction IDs
                        var result = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(txids ?? "[]");
                        return result ?? new List<string>();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get mempool transactions: {ex.Message}");
            }

            return new List<string>();
        }

        public async Task<Transaction?> GetRawTransactionAsync(string transactionId)
        {
            if (_rpcClient == null)
            {
                InitializeClient();
            }

            try
            {
                if (_rpcClient != null)
                {
                    var tx = await _rpcClient.GetRawTransactionAsync(uint256.Parse(transactionId));
                    return tx;
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
            if (_rpcClient == null)
            {
                InitializeClient();
            }

            try
            {
                if (_rpcClient != null)
                {
                    var result = await _rpcClient.SendCommandAsync("getrawtransaction", transactionId, 1);
                    if (result?.Result != null)
                    {
                        var details = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(
                            result.Result.ToString() ?? "{}");
                        return details;
                    }
                }
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
            var seenTxIds = new HashSet<string>();

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var currentTxIds = await GetMempoolTransactionsAsync();

                    foreach (var txId in currentTxIds)
                    {
                        if (!seenTxIds.Contains(txId))
                        {
                            seenTxIds.Add(txId);
                            yield return txId;
                        }
                    }

                    // Clean up old entries if set gets too large
                    if (seenTxIds.Count > 10000)
                    {
                        var toRemove = seenTxIds.Take(5000).ToList();
                        foreach (var txId in toRemove)
                        {
                            seenTxIds.Remove(txId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error monitoring transactions: {ex.Message}");
                }

                // Wait before checking again
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
        }
    }
}
