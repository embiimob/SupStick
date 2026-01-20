using SupStick.Models;
using SupStick.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SupStick.ViewModels
{
    /// <summary>
    /// ViewModel for the Setup page
    /// </summary>
    public class SetupViewModel : BaseViewModel
    {
        private readonly IDataStorageService _dataStorage;
        private readonly IBitcoinService _bitcoinService;
        private readonly IIpfsService _ipfsService;

        private string _newAddress = string.Empty;
        private string _newHandle = string.Empty;
        private string _statusMessage = "Configure settings";
        private bool _isConnected;
        private bool _isIpfsConnected;
        private string _connectionInfo = "Connecting to Bitcoin testnet3 P2P network...";
        private string _ipfsConnectionInfo = "Connecting to IPFS P2P network...";

        public string NewAddress
        {
            get => _newAddress;
            set => SetProperty(ref _newAddress, value);
        }

        public string NewHandle
        {
            get => _newHandle;
            set => SetProperty(ref _newHandle, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        public bool IsIpfsConnected
        {
            get => _isIpfsConnected;
            set => SetProperty(ref _isIpfsConnected, value);
        }

        public string ConnectionInfo
        {
            get => _connectionInfo;
            set => SetProperty(ref _connectionInfo, value);
        }

        public string IpfsConnectionInfo
        {
            get => _ipfsConnectionInfo;
            set => SetProperty(ref _ipfsConnectionInfo, value);
        }

        public ObservableCollection<MonitoredAddress> MonitoredAddresses { get; } = new();
        public ObservableCollection<BlockedAddress> BlockedAddresses { get; } = new();

        public ICommand CheckConnectionCommand { get; }
        public ICommand AddMonitoredAddressCommand { get; }
        public ICommand RemoveMonitoredAddressCommand { get; }
        public ICommand BlockAddressCommand { get; }
        public ICommand UnblockAddressCommand { get; }
        public ICommand ClearAllDataCommand { get; }

        public SetupViewModel(IDataStorageService dataStorage, IBitcoinService bitcoinService, IIpfsService ipfsService)
        {
            _dataStorage = dataStorage;
            _bitcoinService = bitcoinService;
            _ipfsService = ipfsService;

            Title = "Setup";

            CheckConnectionCommand = new Command(async () => await CheckConnectionAsync());
            AddMonitoredAddressCommand = new Command(async () => await AddMonitoredAddressAsync(),
                () => !string.IsNullOrWhiteSpace(NewAddress));
            RemoveMonitoredAddressCommand = new Command<MonitoredAddress>(async (addr) => await RemoveMonitoredAddressAsync(addr));
            BlockAddressCommand = new Command<string>(async (addr) => await BlockAddressAsync(addr));
            UnblockAddressCommand = new Command<BlockedAddress>(async (addr) => await UnblockAddressAsync(addr));
            ClearAllDataCommand = new Command(async () => await ClearAllDataAsync());

            Task.Run(async () => await LoadSettingsAsync());
        }

        private async Task LoadSettingsAsync()
        {
            try
            {
                IsBusy = true;

                // Load monitored addresses
                var monitored = await _dataStorage.GetMonitoredAddressesAsync();
                MonitoredAddresses.Clear();
                foreach (var addr in monitored)
                {
                    MonitoredAddresses.Add(addr);
                }

                // Load blocked addresses
                var blocked = await _dataStorage.GetBlockedAddressesAsync();
                BlockedAddresses.Clear();
                foreach (var addr in blocked)
                {
                    BlockedAddresses.Add(addr);
                }

                // Check connection
                await CheckConnectionAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading settings: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CheckConnectionAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Checking P2P connections...";

                // Check Bitcoin connection
                IsConnected = await _bitcoinService.IsConnectedAsync();
                
                if (IsConnected)
                {
                    ConnectionInfo = "Direct P2P connection established. No RPC server required.";
                }
                else
                {
                    ConnectionInfo = "Connecting to Bitcoin testnet3 peers...";
                }

                // Check IPFS connection
                IsIpfsConnected = await _ipfsService.IsConnectedAsync();
                
                if (IsIpfsConnected)
                {
                    IpfsConnectionInfo = "Direct IPFS P2P connection active.";
                }
                else
                {
                    IpfsConnectionInfo = "Connecting to IPFS peers...";
                }

                var btcStatus = IsConnected ? "✓" : "✗";
                var ipfsStatus = IsIpfsConnected ? "✓" : "✗";
                StatusMessage = $"Bitcoin: {btcStatus} | IPFS: {ipfsStatus}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Connection error: {ex.Message}";
                IsConnected = false;
                IsIpfsConnected = false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task AddMonitoredAddressAsync()
        {
            try
            {
                var address = new MonitoredAddress
                {
                    Address = NewAddress.Trim(),
                    Handle = NewHandle.Trim(),
                    AddedAt = DateTime.UtcNow,
                    IsActive = true
                };

                await _dataStorage.SaveMonitoredAddressAsync(address);
                MonitoredAddresses.Add(address);

                NewAddress = string.Empty;
                NewHandle = string.Empty;
                StatusMessage = "Address added for monitoring";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to add address: {ex.Message}";
            }
        }

        private async Task RemoveMonitoredAddressAsync(MonitoredAddress address)
        {
            try
            {
                await _dataStorage.DeleteMonitoredAddressAsync(address.Id);
                MonitoredAddresses.Remove(address);
                StatusMessage = "Address removed from monitoring";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to remove address: {ex.Message}";
            }
        }

        private async Task BlockAddressAsync(string address)
        {
            try
            {
                await _dataStorage.BlockAddressAsync(address, "Blocked by user");
                await LoadSettingsAsync();
                StatusMessage = "Address blocked";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to block address: {ex.Message}";
            }
        }

        private async Task UnblockAddressAsync(BlockedAddress address)
        {
            try
            {
                await _dataStorage.UnblockAddressAsync(address.Address);
                BlockedAddresses.Remove(address);
                StatusMessage = "Address unblocked";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to unblock address: {ex.Message}";
            }
        }

        private async Task ClearAllDataAsync()
        {
            try
            {
                bool confirm = await Shell.Current.DisplayAlert(
                    "Clear All Data",
                    "Are you sure you want to delete all indexed items? This cannot be undone.",
                    "Yes",
                    "No");

                if (confirm)
                {
                    IsBusy = true;
                    StatusMessage = "Clearing data...";

                    await _dataStorage.DeleteAllIndexedItemsAsync();
                    StatusMessage = "All data cleared";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to clear data: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
