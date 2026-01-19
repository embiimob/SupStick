using SQLite;

namespace SupStick.Models
{
    /// <summary>
    /// Represents a blocked address to prevent indexing unwanted content
    /// </summary>
    [Table("blocked_addresses")]
    public class BlockedAddress
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed, Unique]
        public string Address { get; set; } = string.Empty;

        public DateTime BlockedAt { get; set; }

        public string Reason { get; set; } = string.Empty;
    }
}
