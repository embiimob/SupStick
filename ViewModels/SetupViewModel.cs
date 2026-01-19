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

        private string _rpcUrl = "http://127.0.0.1:18332";
        private string _rpcUsername = "user";
        private string _rpcPassword = "pass";
        private string _newAddress = string.Empty;
        private string _newHandle = string.Empty;
        private string _statusMessage = "Configure settings";
        private bool _isConnected;

        public string RpcUrl
        {
            get => _rpcUrl;
            set => SetProperty(ref _rpcUrl, value);
        }

        public string RpcUsername
        {
            get => _rpcUsername;
            set => SetProperty(ref _rpcUsername, value);
        }

        public string RpcPassword
        {
            get => _rpcPassword;
            set => SetProperty(ref _rpcPassword, value);
        }

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

        public ObservableCollection<MonitoredAddress> MonitoredAddresses { get; } = new();
        public ObservableCollection<BlockedAddress> BlockedAddresses { get; } = new();

        public ICommand TestConnectionCommand { get; }
        public ICommand SaveSettingsCommand { get; }
        public ICommand AddMonitoredAddressCommand { get; }
        public ICommand RemoveMonitoredAddressCommand { get; }
        public ICommand BlockAddressCommand { get; }
        public ICommand UnblockAddressCommand { get; }
        public ICommand ClearAllDataCommand { get; }

        public SetupViewModel(IDataStorageService dataStorage, IBitcoinService bitcoinService)
        {
            _dataStorage = dataStorage;
            _bitcoinService = bitcoinService;

            Title = "Setup";

            TestConnectionCommand = new Command(async () => await TestConnectionAsync());
            SaveSettingsCommand = new Command(async () => await SaveSettingsAsync());
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

                // Load RPC settings
                var url = await _dataStorage.GetSettingAsync("RpcUrl");
                if (!string.IsNullOrEmpty(url))
                    RpcUrl = url;

                var username = await _dataStorage.GetSettingAsync("RpcUsername");
                if (!string.IsNullOrEmpty(username))
                    RpcUsername = username;

                var password = await _dataStorage.GetSettingAsync("RpcPassword");
                if (!string.IsNullOrEmpty(password))
                    RpcPassword = password;

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
                IsConnected = await _bitcoinService.IsConnectedAsync();
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

        private async Task TestConnectionAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Testing connection...";

                // Configure Bitcoin service
                if (_bitcoinService is BitcoinService service)
                {
                    service.Configure(RpcUrl, RpcUsername, RpcPassword);
                }

                IsConnected = await _bitcoinService.IsConnectedAsync();
                StatusMessage = IsConnected ? "Connected successfully!" : "Connection failed";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Connection error: {ex.Message}";
                IsConnected = false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SaveSettingsAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Saving settings...";

                await _dataStorage.SetSettingAsync("RpcUrl", RpcUrl);
                await _dataStorage.SetSettingAsync("RpcUsername", RpcUsername);
                await _dataStorage.SetSettingAsync("RpcPassword", RpcPassword);

                // Configure Bitcoin service
                if (_bitcoinService is BitcoinService service)
                {
                    service.Configure(RpcUrl, RpcUsername, RpcPassword);
                }

                StatusMessage = "Settings saved successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to save settings: {ex.Message}";
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
                bool confirm = await Application.Current!.MainPage!.DisplayAlert(
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
