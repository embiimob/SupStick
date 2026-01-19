using NBitcoin;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SupStick.Services
{
    /// <summary>
    /// Interface for Bitcoin testnet3 operations
    /// </summary>
    public interface IBitcoinService
    {
        /// <summary>
        /// Get pending transactions from mempool
        /// </summary>
        Task<List<string>> GetMempoolTransactionsAsync();

        /// <summary>
        /// Get raw transaction data by transaction ID
        /// </summary>
        Task<Transaction?> GetRawTransactionAsync(string transactionId);

        /// <summary>
        /// Get transaction details including outputs
        /// </summary>
        Task<Dictionary<string, object>?> GetTransactionDetailsAsync(string transactionId);

        /// <summary>
        /// Monitor for new transactions in real-time
        /// </summary>
        IAsyncEnumerable<string> MonitorNewTransactionsAsync();

        /// <summary>
        /// Check if Bitcoin RPC is connected
        /// </summary>
        Task<bool> IsConnectedAsync();
    }
}
