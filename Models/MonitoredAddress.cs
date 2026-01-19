using SQLite;

namespace SupStick.Models
{
    /// <summary>
    /// Represents a monitored address or P2FK handle
    /// </summary>
    [Table("monitored_addresses")]
    public class MonitoredAddress
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public string Address { get; set; } = string.Empty;

        public string Handle { get; set; } = string.Empty; // P2FK handle if applicable

        public DateTime AddedAt { get; set; }

        public bool IsActive { get; set; }
    }
}
