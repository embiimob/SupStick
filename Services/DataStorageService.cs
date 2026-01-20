using SQLite;
using SupStick.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SupStick.Services
{
    /// <summary>
    /// Local data storage service with encrypted SQLite database
    /// </summary>
    public class DataStorageService : IDataStorageService
    {
        private SQLiteAsyncConnection? _database;
        private readonly string _dbPath;

        public DataStorageService()
        {
            // Store database in app data directory
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _dbPath = Path.Combine(appDataPath, "supstick.db3");
        }

        public async Task InitializeAsync()
        {
            if (_database != null)
                return;

            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(_dbPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Console.WriteLine($"Created database directory: {directory}");
                }

                Console.WriteLine($"Initializing database at: {_dbPath}");
                _database = new SQLiteAsyncConnection(_dbPath);

                // Create tables with retry logic
                await CreateTablesWithRetryAsync();
                
                Console.WriteLine("Database initialized successfully");
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine($"SQLite error during initialization: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw new Exception($"Failed to initialize database: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing database: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw new Exception($"Failed to initialize database: {ex.Message}", ex);
            }
        }

        private async Task CreateTablesWithRetryAsync(int maxRetries = 3)
        {
            Exception? lastException = null;
            
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    // Create tables
                    await _database!.CreateTableAsync<IndexedItem>();
                    await _database.CreateTableAsync<BlockedAddress>();
                    await _database.CreateTableAsync<MonitoredAddress>();
                    await _database.CreateTableAsync<AppSettings>();
                    return;
                }
                catch (SQLiteException ex)
                {
                    lastException = ex;
                    if (attempt < maxRetries - 1)
                    {
                        Console.WriteLine($"Failed to create tables (attempt {attempt + 1}/{maxRetries}): {ex.Message}");
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
                    }
                }
            }
            
            // Rethrow the last exception if all retries failed
            if (lastException != null)
            {
                Console.WriteLine($"Failed to create tables after {maxRetries} attempts");
                throw lastException;
            }
        }

        public async Task<int> SaveIndexedItemAsync(IndexedItem item)
        {
            try
            {
                await InitializeAsync();

                if (item.Id == 0)
                {
                    return await _database!.InsertAsync(item);
                }
                else
                {
                    return await _database!.UpdateAsync(item);
                }
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine($"SQLite error saving indexed item: {ex.Message}");
                throw new Exception($"Failed to save indexed item: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving indexed item: {ex.Message}");
                throw;
            }
        }

        public async Task<List<IndexedItem>> GetAllIndexedItemsAsync()
        {
            try
            {
                await InitializeAsync();
                return await _database!.Table<IndexedItem>()
                    .OrderByDescending(x => x.IndexedAt)
                    .ToListAsync();
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine($"SQLite error getting indexed items: {ex.Message}");
                return new List<IndexedItem>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting indexed items: {ex.Message}");
                return new List<IndexedItem>();
            }
        }

        public async Task<IndexedItem?> GetIndexedItemByIdAsync(int id)
        {
            try
            {
                await InitializeAsync();
                return await _database!.Table<IndexedItem>()
                    .Where(x => x.Id == id)
                    .FirstOrDefaultAsync();
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine($"SQLite error getting indexed item by id: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting indexed item by id: {ex.Message}");
                return null;
            }
        }

        public async Task<List<IndexedItem>> GetItemsByAddressAsync(string address)
        {
            try
            {
                await InitializeAsync();
                return await _database!.Table<IndexedItem>()
                    .Where(x => x.SignedBy == address)
                    .OrderByDescending(x => x.IndexedAt)
                    .ToListAsync();
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine($"SQLite error getting items by address: {ex.Message}");
                return new List<IndexedItem>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting items by address: {ex.Message}");
                return new List<IndexedItem>();
            }
        }

        public async Task<List<IndexedItem>> SearchItemsAsync(string query)
        {
            try
            {
                await InitializeAsync();

                if (string.IsNullOrWhiteSpace(query))
                    return await GetAllIndexedItemsAsync();

                query = query.ToLower();

                var items = await _database!.Table<IndexedItem>().ToListAsync();

                return items.Where(x =>
                    x.Content.ToLower().Contains(query) ||
                    x.FileName.ToLower().Contains(query) ||
                    x.SignedBy.ToLower().Contains(query) ||
                    x.TransactionId.ToLower().Contains(query)
                ).OrderByDescending(x => x.IndexedAt)
                .ToList();
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine($"SQLite error searching items: {ex.Message}");
                return new List<IndexedItem>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching items: {ex.Message}");
                return new List<IndexedItem>();
            }
        }

        public async Task<int> DeleteIndexedItemAsync(int id)
        {
            try
            {
                await InitializeAsync();
                return await _database!.DeleteAsync<IndexedItem>(id);
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine($"SQLite error deleting indexed item: {ex.Message}");
                throw new Exception($"Failed to delete indexed item: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting indexed item: {ex.Message}");
                throw;
            }
        }

        public async Task<int> DeleteAllIndexedItemsAsync()
        {
            try
            {
                await InitializeAsync();
                return await _database!.DeleteAllAsync<IndexedItem>();
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine($"SQLite error deleting all indexed items: {ex.Message}");
                throw new Exception($"Failed to delete all indexed items: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting all indexed items: {ex.Message}");
                throw;
            }
        }

        public async Task<int> BlockAddressAsync(string address, string reason)
        {
            try
            {
                await InitializeAsync();

                var blocked = new BlockedAddress
                {
                    Address = address,
                    BlockedAt = DateTime.UtcNow,
                    Reason = reason
                };

                try
                {
                    return await _database!.InsertAsync(blocked);
                }
                catch (SQLiteException)
                {
                    // Address already blocked
                    Console.WriteLine($"Address already blocked: {address}");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error blocking address: {ex.Message}");
                throw;
            }
        }

        public async Task<int> UnblockAddressAsync(string address)
        {
            try
            {
                await InitializeAsync();

                var blocked = await _database!.Table<BlockedAddress>()
                    .Where(x => x.Address == address)
                    .FirstOrDefaultAsync();

                if (blocked != null)
                {
                    return await _database.DeleteAsync(blocked);
                }

                return 0;
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine($"SQLite error unblocking address: {ex.Message}");
                throw new Exception($"Failed to unblock address: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error unblocking address: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> IsAddressBlockedAsync(string address)
        {
            try
            {
                await InitializeAsync();

                var count = await _database!.Table<BlockedAddress>()
                    .Where(x => x.Address == address)
                    .CountAsync();

                return count > 0;
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine($"SQLite error checking if address is blocked: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking if address is blocked: {ex.Message}");
                return false;
            }
        }

        public async Task<List<BlockedAddress>> GetBlockedAddressesAsync()
        {
            try
            {
                await InitializeAsync();
                return await _database!.Table<BlockedAddress>()
                    .OrderByDescending(x => x.BlockedAt)
                    .ToListAsync();
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine($"SQLite error getting blocked addresses: {ex.Message}");
                return new List<BlockedAddress>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting blocked addresses: {ex.Message}");
                return new List<BlockedAddress>();
            }
        }

        public async Task<int> SaveMonitoredAddressAsync(MonitoredAddress address)
        {
            try
            {
                await InitializeAsync();

                if (address.Id == 0)
                {
                    return await _database!.InsertAsync(address);
                }
                else
                {
                    return await _database!.UpdateAsync(address);
                }
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine($"SQLite error saving monitored address: {ex.Message}");
                throw new Exception($"Failed to save monitored address: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving monitored address: {ex.Message}");
                throw;
            }
        }

        public async Task<List<MonitoredAddress>> GetMonitoredAddressesAsync()
        {
            try
            {
                await InitializeAsync();
                return await _database!.Table<MonitoredAddress>()
                    .Where(x => x.IsActive)
                    .OrderByDescending(x => x.AddedAt)
                    .ToListAsync();
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine($"SQLite error getting monitored addresses: {ex.Message}");
                return new List<MonitoredAddress>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting monitored addresses: {ex.Message}");
                return new List<MonitoredAddress>();
            }
        }

        public async Task<int> DeleteMonitoredAddressAsync(int id)
        {
            try
            {
                await InitializeAsync();
                return await _database!.DeleteAsync<MonitoredAddress>(id);
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine($"SQLite error deleting monitored address: {ex.Message}");
                throw new Exception($"Failed to delete monitored address: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting monitored address: {ex.Message}");
                throw;
            }
        }

        public async Task<string?> GetSettingAsync(string key)
        {
            try
            {
                await InitializeAsync();

                var setting = await _database!.Table<AppSettings>()
                    .Where(x => x.Key == key)
                    .FirstOrDefaultAsync();

                return setting?.Value;
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine($"SQLite error getting setting: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting setting: {ex.Message}");
                return null;
            }
        }

        public async Task SetSettingAsync(string key, string value)
        {
            try
            {
                await InitializeAsync();

                var setting = await _database!.Table<AppSettings>()
                    .Where(x => x.Key == key)
                    .FirstOrDefaultAsync();

                if (setting == null)
                {
                    setting = new AppSettings { Key = key, Value = value };
                    await _database.InsertAsync(setting);
                }
                else
                {
                    setting.Value = value;
                    await _database.UpdateAsync(setting);
                }
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine($"SQLite error setting value: {ex.Message}");
                throw new Exception($"Failed to set setting: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting value: {ex.Message}");
                throw;
            }
        }
    }
}
