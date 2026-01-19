using SupStick.Models;
using SupStick.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SupStick.ViewModels
{
    /// <summary>
    /// ViewModel for the Status page showing latest indexed items
    /// </summary>
    public class StatusViewModel : BaseViewModel
    {
        private readonly IDataStorageService _dataStorage;
        private readonly ITransactionMonitorService _monitorService;
        private readonly IBitcoinService _bitcoinService;

        private string _statusMessage = "Ready";
        private bool _isMonitoring;
        private int _itemCount;

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsMonitoringActive
        {
            get => _isMonitoring;
            set => SetProperty(ref _isMonitoring, value);
        }

        public int ItemCount
        {
            get => _itemCount;
            set => SetProperty(ref _itemCount, value);
        }

        public ObservableCollection<IndexedItem> RecentItems { get; } = new();

        public ICommand RefreshCommand { get; }
        public ICommand StartMonitoringCommand { get; }
        public ICommand StopMonitoringCommand { get; }
        public ICommand DeleteItemCommand { get; }

        public StatusViewModel(
            IDataStorageService dataStorage,
            ITransactionMonitorService monitorService,
            IBitcoinService bitcoinService)
        {
            _dataStorage = dataStorage;
            _monitorService = monitorService;
            _bitcoinService = bitcoinService;

            Title = "Status";

            RefreshCommand = new Command(async () => await RefreshAsync());
            StartMonitoringCommand = new Command(async () => await StartMonitoringAsync(), () => !IsMonitoringActive);
            StopMonitoringCommand = new Command(async () => await StopMonitoringAsync(), () => IsMonitoringActive);
            DeleteItemCommand = new Command<IndexedItem>(async (item) => await DeleteItemAsync(item));

            // Subscribe to monitoring events
            _monitorService.StatusChanged += OnMonitoringStatusChanged;
            _monitorService.ItemIndexed += OnItemIndexed;

            // Load initial data
            Task.Run(async () => await LoadDataAsync());
        }

        private async Task LoadDataAsync()
        {
            try
            {
                IsBusy = true;

                var items = await _dataStorage.GetAllIndexedItemsAsync();
                ItemCount = items.Count;

                RecentItems.Clear();
                foreach (var item in items.Take(50)) // Show latest 50
                {
                    RecentItems.Add(item);
                }

                IsMonitoringActive = _monitorService.IsMonitoring;

                // Check Bitcoin connection
                var isConnected = await _bitcoinService.IsConnectedAsync();
                StatusMessage = isConnected ? "Connected to Bitcoin testnet3" : "Not connected to Bitcoin RPC";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RefreshAsync()
        {
            await LoadDataAsync();
        }

        private async Task StartMonitoringAsync()
        {
            try
            {
                StatusMessage = "Starting monitoring...";
                _ = Task.Run(async () => await _monitorService.StartMonitoringAsync());
                await Task.Delay(1000); // Give it a moment to start
                ((Command)StartMonitoringCommand).ChangeCanExecute();
                ((Command)StopMonitoringCommand).ChangeCanExecute();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to start: {ex.Message}";
            }
        }

        private async Task StopMonitoringAsync()
        {
            try
            {
                await _monitorService.StopMonitoringAsync();
                ((Command)StartMonitoringCommand).ChangeCanExecute();
                ((Command)StopMonitoringCommand).ChangeCanExecute();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to stop: {ex.Message}";
            }
        }

        private async Task DeleteItemAsync(IndexedItem item)
        {
            try
            {
                await _dataStorage.DeleteIndexedItemAsync(item.Id);
                RecentItems.Remove(item);
                ItemCount--;
                StatusMessage = "Item deleted";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to delete: {ex.Message}";
            }
        }

        private void OnMonitoringStatusChanged(object? sender, MonitoringStatusEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsMonitoringActive = e.IsActive;
                StatusMessage = e.Message;
                ((Command)StartMonitoringCommand).ChangeCanExecute();
                ((Command)StopMonitoringCommand).ChangeCanExecute();
            });
        }

        private void OnItemIndexed(object? sender, ItemIndexedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await RefreshAsync();
                StatusMessage = $"New {e.Type} indexed: {e.Content}";
            });
        }
    }
}
