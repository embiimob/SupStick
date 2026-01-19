using SQLite;
using System;

namespace SupStick.Models
{
    /// <summary>
    /// Represents an indexed message or file from P2FK transactions
    /// </summary>
    [Table("indexed_items")]
    public class IndexedItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public string TransactionId { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty; // "message", "file", "ipfs"

        public string Content { get; set; } = string.Empty; // Text message or file path

        public string IpfsHash { get; set; } = string.Empty; // IPFS hash if applicable

        public string FileName { get; set; } = string.Empty;

        public long FileSize { get; set; }

        [Indexed]
        public string SignedBy { get; set; } = string.Empty; // Address that signed this

        public DateTime IndexedAt { get; set; }

        public DateTime BlockDate { get; set; }

        public int BlockHeight { get; set; }

        public int Confirmations { get; set; }

        public bool IsDownloaded { get; set; } // For IPFS files

        public string LocalPath { get; set; } = string.Empty; // Local file path
    }
}
