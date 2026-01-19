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
    /// Media player service implementation
    /// </summary>
    public class MediaPlayerService : IMediaPlayerService
    {
        private readonly IDataStorageService _dataStorage;
        private SQLiteAsyncConnection? _database;
        private readonly string _dbPath;

        private readonly HashSet<string> _audioExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".wav", ".ogg", ".m4a", ".aac", ".flac", ".wma", ".opus"
        };

        private readonly HashSet<string> _videoExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg"
        };

        public MediaPlayerService(IDataStorageService dataStorage)
        {
            _dataStorage = dataStorage;
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _dbPath = Path.Combine(appDataPath, "supstick.db3");
        }

        private async Task InitializeAsync()
        {
            if (_database != null)
                return;

            _database = new SQLiteAsyncConnection(_dbPath);
            await _database.CreateTableAsync<Playlist>();
            await _database.CreateTableAsync<PlaylistItem>();
        }

        public bool IsMediaFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            var extension = Path.GetExtension(fileName);
            return _audioExtensions.Contains(extension) || _videoExtensions.Contains(extension);
        }

        public string GetMediaType(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "unknown";

            var extension = Path.GetExtension(fileName);

            if (_audioExtensions.Contains(extension))
                return "audio";

            if (_videoExtensions.Contains(extension))
                return "video";

            return "unknown";
        }

        public async Task<List<MediaItem>> GetMediaItemsAsync()
        {
            var indexedItems = await _dataStorage.GetAllIndexedItemsAsync();
            var mediaItems = new List<MediaItem>();

            foreach (var item in indexedItems)
            {
                if (!string.IsNullOrEmpty(item.FileName) && IsMediaFile(item.FileName))
                {
                    var mediaItem = new MediaItem
                    {
                        Id = item.Id,
                        Title = Path.GetFileNameWithoutExtension(item.FileName),
                        FilePath = item.LocalPath,
                        MediaType = GetMediaType(item.FileName),
                        FileExtension = Path.GetExtension(item.FileName),
                        FileSize = item.FileSize,
                        AddedAt = item.IndexedAt,
                        Source = string.IsNullOrEmpty(item.IpfsHash) ? "local" : "ipfs",
                        IpfsHash = item.IpfsHash
                    };

                    mediaItems.Add(mediaItem);
                }
            }

            return mediaItems.OrderByDescending(m => m.AddedAt).ToList();
        }

        public async Task<List<MediaItem>> GetMediaItemsByTypeAsync(string mediaType)
        {
            var allMedia = await GetMediaItemsAsync();
            return allMedia.Where(m => m.MediaType == mediaType).ToList();
        }

        public async Task<int> CreatePlaylistAsync(Playlist playlist)
        {
            await InitializeAsync();

            playlist.CreatedAt = DateTime.UtcNow;
            playlist.UpdatedAt = DateTime.UtcNow;

            return await _database!.InsertAsync(playlist);
        }

        public async Task<List<Playlist>> GetPlaylistsAsync()
        {
            await InitializeAsync();
            return await _database!.Table<Playlist>()
                .OrderByDescending(p => p.UpdatedAt)
                .ToListAsync();
        }

        public async Task<Playlist?> GetPlaylistByIdAsync(int id)
        {
            await InitializeAsync();
            return await _database!.Table<Playlist>()
                .Where(p => p.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<int> UpdatePlaylistAsync(Playlist playlist)
        {
            await InitializeAsync();
            playlist.UpdatedAt = DateTime.UtcNow;
            return await _database!.UpdateAsync(playlist);
        }

        public async Task<int> DeletePlaylistAsync(int id)
        {
            await InitializeAsync();

            // Delete all playlist items
            var items = await _database!.Table<PlaylistItem>()
                .Where(p => p.PlaylistId == id)
                .ToListAsync();

            foreach (var item in items)
            {
                await _database.DeleteAsync(item);
            }

            // Delete the playlist
            return await _database.DeleteAsync<Playlist>(id);
        }

        public async Task<int> AddItemToPlaylistAsync(int playlistId, int indexedItemId)
        {
            await InitializeAsync();

            // Get current max order
            var maxOrder = 0;
            var items = await _database!.Table<PlaylistItem>()
                .Where(p => p.PlaylistId == playlistId)
                .ToListAsync();

            if (items.Count > 0)
            {
                maxOrder = items.Max(i => i.Order);
            }

            var playlistItem = new PlaylistItem
            {
                PlaylistId = playlistId,
                IndexedItemId = indexedItemId,
                Order = maxOrder + 1,
                AddedAt = DateTime.UtcNow
            };

            var result = await _database.InsertAsync(playlistItem);

            // Update playlist timestamp
            var playlist = await GetPlaylistByIdAsync(playlistId);
            if (playlist != null)
            {
                await UpdatePlaylistAsync(playlist);
            }

            return result;
        }

        public async Task<int> RemoveItemFromPlaylistAsync(int playlistItemId)
        {
            await InitializeAsync();
            return await _database!.DeleteAsync<PlaylistItem>(playlistItemId);
        }

        public async Task<List<MediaItem>> GetPlaylistItemsAsync(int playlistId)
        {
            await InitializeAsync();

            var playlistItems = await _database!.Table<PlaylistItem>()
                .Where(p => p.PlaylistId == playlistId)
                .OrderBy(p => p.Order)
                .ToListAsync();

            var mediaItems = new List<MediaItem>();

            foreach (var item in playlistItems)
            {
                var indexedItem = await _dataStorage.GetIndexedItemByIdAsync(item.IndexedItemId);

                if (indexedItem != null && !string.IsNullOrEmpty(indexedItem.FileName) && IsMediaFile(indexedItem.FileName))
                {
                    var mediaItem = new MediaItem
                    {
                        Id = indexedItem.Id,
                        Title = Path.GetFileNameWithoutExtension(indexedItem.FileName),
                        FilePath = indexedItem.LocalPath,
                        MediaType = GetMediaType(indexedItem.FileName),
                        FileExtension = Path.GetExtension(indexedItem.FileName),
                        FileSize = indexedItem.FileSize,
                        AddedAt = indexedItem.IndexedAt,
                        Source = string.IsNullOrEmpty(indexedItem.IpfsHash) ? "local" : "ipfs",
                        IpfsHash = indexedItem.IpfsHash
                    };

                    mediaItems.Add(mediaItem);
                }
            }

            return mediaItems;
        }

        public async Task ReorderPlaylistItemsAsync(int playlistId, List<int> itemIds)
        {
            await InitializeAsync();

            var items = await _database!.Table<PlaylistItem>()
                .Where(p => p.PlaylistId == playlistId)
                .ToListAsync();

            for (int i = 0; i < itemIds.Count; i++)
            {
                var item = items.FirstOrDefault(p => p.IndexedItemId == itemIds[i]);
                if (item != null)
                {
                    item.Order = i + 1;
                    await _database.UpdateAsync(item);
                }
            }

            // Update playlist timestamp
            var playlist = await GetPlaylistByIdAsync(playlistId);
            if (playlist != null)
            {
                await UpdatePlaylistAsync(playlist);
            }
        }
    }
}
