namespace SupStick.Models
{
    /// <summary>
    /// Represents a media item for playback
    /// </summary>
    public class MediaItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string MediaType { get; set; } = string.Empty; // "audio" or "video"
        public string FileExtension { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public TimeSpan Duration { get; set; }
        public string ThumbnailPath { get; set; } = string.Empty;
        public DateTime AddedAt { get; set; }
        public string Source { get; set; } = string.Empty; // "local", "ipfs"
        public string IpfsHash { get; set; } = string.Empty;
    }
}
