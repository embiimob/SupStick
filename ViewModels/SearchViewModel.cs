using SupStick.Models;
using SupStick.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SupStick.ViewModels
{
    /// <summary>
    /// ViewModel for the Search page
    /// </summary>
    public class SearchViewModel : BaseViewModel
    {
        private readonly IDataStorageService _dataStorage;

        private string _searchQuery = string.Empty;
        private string _statusMessage = "Enter address or P2FK handle to search";

        public string SearchQuery
        {
            get => _searchQuery;
            set => SetProperty(ref _searchQuery, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ObservableCollection<IndexedItem> SearchResults { get; } = new();

        public ICommand SearchCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand DeleteItemCommand { get; }

        public SearchViewModel(IDataStorageService dataStorage)
        {
            _dataStorage = dataStorage;
            Title = "Search";

            SearchCommand = new Command(async () => await SearchAsync(), () => !string.IsNullOrWhiteSpace(SearchQuery));
            ClearCommand = new Command(Clear);
            DeleteItemCommand = new Command<IndexedItem>(async (item) => await DeleteItemAsync(item));
        }

        private async Task SearchAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Searching...";

                SearchResults.Clear();

                var results = await _dataStorage.SearchItemsAsync(SearchQuery);

                if (results.Count == 0)
                {
                    StatusMessage = "No results found";
                }
                else
                {
                    foreach (var item in results)
                    {
                        SearchResults.Add(item);
                    }
                    StatusMessage = $"Found {results.Count} result(s)";
                }
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

        private void Clear()
        {
            SearchQuery = string.Empty;
            SearchResults.Clear();
            StatusMessage = "Enter address or P2FK handle to search";
        }

        private async Task DeleteItemAsync(IndexedItem item)
        {
            try
            {
                await _dataStorage.DeleteIndexedItemAsync(item.Id);
                SearchResults.Remove(item);
                StatusMessage = "Item deleted";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to delete: {ex.Message}";
            }
        }
    }
}
