using System;
using System.Collections.Generic;
using System.Numerics;

namespace SupStick.Models
{
    /// <summary>
    /// P2FK Root message model - adapted from the Sup repository
    /// </summary>
    public class P2FKRoot
    {
        public int Id { get; set; }
        public string[] Message { get; set; } = Array.Empty<string>();
        public Dictionary<string, BigInteger> File { get; set; } = new Dictionary<string, BigInteger>();
        public Dictionary<string, string> Keyword { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Output { get; set; } = new Dictionary<string, string>();
        public string Hash { get; set; } = string.Empty;
        public string SignedBy { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
        public bool Signed { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public DateTime BlockDate { get; set; }
        public int BlockHeight { get; set; }
        public int TotalByteSize { get; set; }
        public int Confirmations { get; set; }
        public DateTime BuildDate { get; set; }
        public bool Cached { get; set; }
    }
}
