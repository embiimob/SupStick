using System;
using System.Threading;
using System.Threading.Tasks;

namespace SupStick.Services
{
    /// <summary>
    /// Interface for transaction monitoring service
    /// </summary>
    public interface ITransactionMonitorService
    {
        /// <summary>
        /// Start monitoring transactions
        /// </summary>
        Task StartMonitoringAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stop monitoring transactions
        /// </summary>
        Task StopMonitoringAsync();

        /// <summary>
        /// Check if monitoring is active
        /// </summary>
        bool IsMonitoring { get; }

        /// <summary>
        /// Event raised when a new item is indexed
        /// </summary>
        event EventHandler<ItemIndexedEventArgs>? ItemIndexed;

        /// <summary>
        /// Event raised when monitoring status changes
        /// </summary>
        event EventHandler<MonitoringStatusEventArgs>? StatusChanged;
    }

    public class ItemIndexedEventArgs : EventArgs
    {
        public string TransactionId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class MonitoringStatusEventArgs : EventArgs
    {
        public bool IsActive { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
