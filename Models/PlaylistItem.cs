using SQLite;

namespace SupStick.Models
{
    /// <summary>
    /// Represents an item in a playlist
    /// </summary>
    [Table("playlist_items")]
    public class PlaylistItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int PlaylistId { get; set; }

        [Indexed]
        public int IndexedItemId { get; set; }

        public int Order { get; set; }

        public DateTime AddedAt { get; set; }
    }
}
