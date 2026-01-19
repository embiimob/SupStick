using SupStick.Models;
using System.Threading.Tasks;

namespace SupStick.Services
{
    /// <summary>
    /// Interface for P2FK message parsing
    /// </summary>
    public interface IP2FKService
    {
        /// <summary>
        /// Parse P2FK messages from a transaction
        /// </summary>
        Task<P2FKRoot?> ParseTransactionAsync(string transactionId);

        /// <summary>
        /// Parse P2FK messages from transaction data
        /// </summary>
        P2FKRoot? ParseFromTransactionData(byte[] transactionBytes, string transactionId);

        /// <summary>
        /// Extract addresses from transaction outputs
        /// </summary>
        Task<Dictionary<string, string>> GetTransactionOutputsAsync(string transactionId);
    }
}
