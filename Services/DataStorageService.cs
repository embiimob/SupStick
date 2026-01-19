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

            _database = new SQLiteAsyncConnection(_dbPath);

            // Create tables
            await _database.CreateTableAsync<IndexedItem>();
            await _database.CreateTableAsync<BlockedAddress>();
            await _database.CreateTableAsync<MonitoredAddress>();
            await _database.CreateTableAsync<AppSettings>();
        }

        public async Task<int> SaveIndexedItemAsync(IndexedItem item)
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

        public async Task<List<IndexedItem>> GetAllIndexedItemsAsync()
        {
            await InitializeAsync();
            return await _database!.Table<IndexedItem>()
                .OrderByDescending(x => x.IndexedAt)
                .ToListAsync();
        }

        public async Task<List<IndexedItem>> GetItemsByAddressAsync(string address)
        {
            await InitializeAsync();
            return await _database!.Table<IndexedItem>()
                .Where(x => x.SignedBy == address)
                .OrderByDescending(x => x.IndexedAt)
                .ToListAsync();
        }

        public async Task<List<IndexedItem>> SearchItemsAsync(string query)
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

        public async Task<int> DeleteIndexedItemAsync(int id)
        {
            await InitializeAsync();
            return await _database!.DeleteAsync<IndexedItem>(id);
        }

        public async Task<int> DeleteAllIndexedItemsAsync()
        {
            await InitializeAsync();
            return await _database!.DeleteAllAsync<IndexedItem>();
        }

        public async Task<int> BlockAddressAsync(string address, string reason)
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
                return 0;
            }
        }

        public async Task<int> UnblockAddressAsync(string address)
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

        public async Task<bool> IsAddressBlockedAsync(string address)
        {
            await InitializeAsync();

            var count = await _database!.Table<BlockedAddress>()
                .Where(x => x.Address == address)
                .CountAsync();

            return count > 0;
        }

        public async Task<List<BlockedAddress>> GetBlockedAddressesAsync()
        {
            await InitializeAsync();
            return await _database!.Table<BlockedAddress>()
                .OrderByDescending(x => x.BlockedAt)
                .ToListAsync();
        }

        public async Task<int> SaveMonitoredAddressAsync(MonitoredAddress address)
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

        public async Task<List<MonitoredAddress>> GetMonitoredAddressesAsync()
        {
            await InitializeAsync();
            return await _database!.Table<MonitoredAddress>()
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.AddedAt)
                .ToListAsync();
        }

        public async Task<int> DeleteMonitoredAddressAsync(int id)
        {
            await InitializeAsync();
            return await _database!.DeleteAsync<MonitoredAddress>(id);
        }

        public async Task<string?> GetSettingAsync(string key)
        {
            await InitializeAsync();

            var setting = await _database!.Table<AppSettings>()
                .Where(x => x.Key == key)
                .FirstOrDefaultAsync();

            return setting?.Value;
        }

        public async Task SetSettingAsync(string key, string value)
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
    }
}
