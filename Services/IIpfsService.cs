using System.Threading.Tasks;

namespace SupStick.Services
{
    /// <summary>
    /// Interface for IPFS operations
    /// </summary>
    public interface IIpfsService
    {
        /// <summary>
        /// Download file from IPFS gateway
        /// </summary>
        Task<byte[]?> DownloadFileAsync(string ipfsHash);

        /// <summary>
        /// Download file with retry mechanism
        /// </summary>
        Task<byte[]?> DownloadFileWithRetryAsync(string ipfsHash, int maxRetries = 3);

        /// <summary>
        /// Get IPFS gateway URL
        /// </summary>
        string GetGatewayUrl(string ipfsHash);

        /// <summary>
        /// Extract IPFS hash from message content
        /// </summary>
        string? ExtractIpfsHash(string content);
    }
}
