using SQLite;
using System;

namespace SupStick.Models
{
    /// <summary>
    /// Represents a media playlist
    /// </summary>
    [Table("playlists")]
    public class Playlist
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public string Type { get; set; } = string.Empty; // "audio", "video", "mixed"
    }
}
