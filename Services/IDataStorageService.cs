using SupStick.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SupStick.Services
{
    /// <summary>
    /// Interface for local data storage operations
    /// </summary>
    public interface IDataStorageService
    {
        /// <summary>
        /// Initialize the database
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Save an indexed item
        /// </summary>
        Task<int> SaveIndexedItemAsync(IndexedItem item);

        /// <summary>
        /// Get all indexed items
        /// </summary>
        Task<List<IndexedItem>> GetAllIndexedItemsAsync();

        /// <summary>
        /// Get indexed items by address
        /// </summary>
        Task<List<IndexedItem>> GetItemsByAddressAsync(string address);

        /// <summary>
        /// Search indexed items
        /// </summary>
        Task<List<IndexedItem>> SearchItemsAsync(string query);

        /// <summary>
        /// Delete an indexed item
        /// </summary>
        Task<int> DeleteIndexedItemAsync(int id);

        /// <summary>
        /// Delete all indexed items
        /// </summary>
        Task<int> DeleteAllIndexedItemsAsync();

        /// <summary>
        /// Block an address
        /// </summary>
        Task<int> BlockAddressAsync(string address, string reason);

        /// <summary>
        /// Unblock an address
        /// </summary>
        Task<int> UnblockAddressAsync(string address);

        /// <summary>
        /// Check if an address is blocked
        /// </summary>
        Task<bool> IsAddressBlockedAsync(string address);

        /// <summary>
        /// Get all blocked addresses
        /// </summary>
        Task<List<BlockedAddress>> GetBlockedAddressesAsync();

        /// <summary>
        /// Save a monitored address
        /// </summary>
        Task<int> SaveMonitoredAddressAsync(MonitoredAddress address);

        /// <summary>
        /// Get all monitored addresses
        /// </summary>
        Task<List<MonitoredAddress>> GetMonitoredAddressesAsync();

        /// <summary>
        /// Delete a monitored address
        /// </summary>
        Task<int> DeleteMonitoredAddressAsync(int id);

        /// <summary>
        /// Get or set a setting value
        /// </summary>
        Task<string?> GetSettingAsync(string key);
        Task SetSettingAsync(string key, string value);
    }
}
