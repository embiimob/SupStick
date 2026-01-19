using SQLite;

namespace SupStick.Models
{
    /// <summary>
    /// Configuration settings for the application
    /// </summary>
    [Table("settings")]
    public class AppSettings
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Key { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;
    }
}
