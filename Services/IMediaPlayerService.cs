using SupStick.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SupStick.Services
{
    /// <summary>
    /// Interface for media player service
    /// </summary>
    public interface IMediaPlayerService
    {
        /// <summary>
        /// Get all media items from indexed files
        /// </summary>
        Task<List<MediaItem>> GetMediaItemsAsync();

        /// <summary>
        /// Get media items by type (audio/video)
        /// </summary>
        Task<List<MediaItem>> GetMediaItemsByTypeAsync(string mediaType);

        /// <summary>
        /// Create a new playlist
        /// </summary>
        Task<int> CreatePlaylistAsync(Playlist playlist);

        /// <summary>
        /// Get all playlists
        /// </summary>
        Task<List<Playlist>> GetPlaylistsAsync();

        /// <summary>
        /// Get playlist by ID
        /// </summary>
        Task<Playlist?> GetPlaylistByIdAsync(int id);

        /// <summary>
        /// Update playlist
        /// </summary>
        Task<int> UpdatePlaylistAsync(Playlist playlist);

        /// <summary>
        /// Delete playlist
        /// </summary>
        Task<int> DeletePlaylistAsync(int id);

        /// <summary>
        /// Add item to playlist
        /// </summary>
        Task<int> AddItemToPlaylistAsync(int playlistId, int indexedItemId);

        /// <summary>
        /// Remove item from playlist
        /// </summary>
        Task<int> RemoveItemFromPlaylistAsync(int playlistItemId);

        /// <summary>
        /// Get items in a playlist
        /// </summary>
        Task<List<MediaItem>> GetPlaylistItemsAsync(int playlistId);

        /// <summary>
        /// Reorder items in playlist
        /// </summary>
        Task ReorderPlaylistItemsAsync(int playlistId, List<int> itemIds);

        /// <summary>
        /// Check if file is a media file
        /// </summary>
        bool IsMediaFile(string fileName);

        /// <summary>
        /// Determine media type from file extension
        /// </summary>
        string GetMediaType(string fileName);
    }
}
