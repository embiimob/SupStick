using SupStick.Models;
using SupStick.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SupStick.ViewModels
{
    /// <summary>
    /// ViewModel for the Media Player page with seamless audio/video playback
    /// </summary>
    public class MediaPlayerViewModel : BaseViewModel
    {
        private readonly IMediaPlayerService _mediaPlayerService;
        private readonly IDataStorageService _dataStorage;

        private MediaItem? _currentMediaItem;
        private Playlist? _currentPlaylist;
        private int _currentPlaylistIndex = -1;
        private bool _isPlaying;
        private bool _isPaused;
        private bool _isFullScreen;
        private double _currentPosition;
        private double _duration;
        private double _volume = 0.8;
        private bool _isRepeatEnabled;
        private bool _isShuffleEnabled;
        private string _statusMessage = "Ready";
        private string _playlistName = string.Empty;
        private string _playlistDescription = string.Empty;
        private string _filterType = "all"; // "all", "audio", "video"
        private bool _showPlaylistEditor;
        private int _selectedTabIndex = 0; // 0=Library, 1=Queue, 2=Playlists

        public MediaItem? CurrentMediaItem
        {
            get => _currentMediaItem;
            set => SetProperty(ref _currentMediaItem, value);
        }

        public Playlist? CurrentPlaylist
        {
            get => _currentPlaylist;
            set => SetProperty(ref _currentPlaylist, value);
        }

        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                if (SetProperty(ref _isPlaying, value))
                {
                    ((Command)PlayCommand).ChangeCanExecute();
                    ((Command)PauseCommand).ChangeCanExecute();
                    ((Command)StopCommand).ChangeCanExecute();
                }
            }
        }

        public bool IsPaused
        {
            get => _isPaused;
            set => SetProperty(ref _isPaused, value);
        }

        public bool IsFullScreen
        {
            get => _isFullScreen;
            set => SetProperty(ref _isFullScreen, value);
        }

        public double CurrentPosition
        {
            get => _currentPosition;
            set => SetProperty(ref _currentPosition, value);
        }

        public double Duration
        {
            get => _duration;
            set => SetProperty(ref _duration, value);
        }

        public double Volume
        {
            get => _volume;
            set => SetProperty(ref _volume, value);
        }

        public bool IsRepeatEnabled
        {
            get => _isRepeatEnabled;
            set => SetProperty(ref _isRepeatEnabled, value);
        }

        public bool IsShuffleEnabled
        {
            get => _isShuffleEnabled;
            set => SetProperty(ref _isShuffleEnabled, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string PlaylistName
        {
            get => _playlistName;
            set => SetProperty(ref _playlistName, value);
        }

        public string PlaylistDescription
        {
            get => _playlistDescription;
            set => SetProperty(ref _playlistDescription, value);
        }

        public string FilterType
        {
            get => _filterType;
            set
            {
                if (SetProperty(ref _filterType, value))
                {
                    Task.Run(async () => await LoadMediaItemsAsync());
                }
            }
        }

        public bool ShowPlaylistEditor
        {
            get => _showPlaylistEditor;
            set => SetProperty(ref _showPlaylistEditor, value);
        }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (SetProperty(ref _selectedTabIndex, value))
                {
                    OnPropertyChanged(nameof(IsLibraryTabSelected));
                    OnPropertyChanged(nameof(IsQueueTabSelected));
                    OnPropertyChanged(nameof(IsPlaylistsTabSelected));
                }
            }
        }

        public bool IsLibraryTabSelected => SelectedTabIndex == 0;
        public bool IsQueueTabSelected => SelectedTabIndex == 1;
        public bool IsPlaylistsTabSelected => SelectedTabIndex == 2;

        public string CurrentPositionText => TimeSpan.FromSeconds(CurrentPosition).ToString(@"mm\:ss");
        public string DurationText => TimeSpan.FromSeconds(Duration).ToString(@"mm\:ss");
        public string CurrentMediaTitle => CurrentMediaItem?.Title ?? "No media loaded";
        public string CurrentMediaType => CurrentMediaItem?.MediaType ?? "";
        public bool IsVideoPlaying => CurrentMediaItem?.MediaType == "video" && IsPlaying && !IsPaused;
        public bool IsAudioPlaying => CurrentMediaItem?.MediaType == "audio" && IsPlaying && !IsPaused;

        public ObservableCollection<MediaItem> MediaLibrary { get; } = new();
        public ObservableCollection<MediaItem> CurrentPlaylistItems { get; } = new();
        public ObservableCollection<Playlist> Playlists { get; } = new();

        public ICommand PlayCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand NextCommand { get; }
        public ICommand PreviousCommand { get; }
        public ICommand ToggleFullScreenCommand { get; }
        public ICommand ToggleRepeatCommand { get; }
        public ICommand ToggleShuffleCommand { get; }
        public ICommand PlayMediaItemCommand { get; }
        public ICommand AddToQueueCommand { get; }
        public ICommand CreatePlaylistCommand { get; }
        public ICommand LoadPlaylistCommand { get; }
        public ICommand SavePlaylistCommand { get; }
        public ICommand DeletePlaylistCommand { get; }
        public ICommand AddToPlaylistCommand { get; }
        public ICommand RemoveFromPlaylistCommand { get; }
        public ICommand RefreshLibraryCommand { get; }
        public ICommand TogglePlaylistEditorCommand { get; }
        public ICommand SelectTabCommand { get; }

        public MediaPlayerViewModel(
            IMediaPlayerService mediaPlayerService,
            IDataStorageService dataStorage)
        {
            _mediaPlayerService = mediaPlayerService;
            _dataStorage = dataStorage;

            Title = "Media Player";

            PlayCommand = new Command(Play, () => CurrentMediaItem != null && !IsPlaying);
            PauseCommand = new Command(Pause, () => IsPlaying && !IsPaused);
            StopCommand = new Command(Stop, () => IsPlaying || IsPaused);
            NextCommand = new Command(PlayNext);
            PreviousCommand = new Command(PlayPrevious);
            ToggleFullScreenCommand = new Command(ToggleFullScreen);
            ToggleRepeatCommand = new Command(ToggleRepeat);
            ToggleShuffleCommand = new Command(ToggleShuffle);
            PlayMediaItemCommand = new Command<MediaItem>(async (item) => await PlayMediaItemAsync(item));
            AddToQueueCommand = new Command<MediaItem>(AddToQueue);
            CreatePlaylistCommand = new Command(async () => await CreatePlaylistAsync());
            LoadPlaylistCommand = new Command<Playlist>(async (playlist) => await LoadPlaylistAsync(playlist));
            SavePlaylistCommand = new Command(async () => await SaveCurrentPlaylistAsync());
            DeletePlaylistCommand = new Command<Playlist>(async (playlist) => await DeletePlaylistAsync(playlist));
            AddToPlaylistCommand = new Command<MediaItem>(AddToCurrentPlaylist);
            RemoveFromPlaylistCommand = new Command<MediaItem>(RemoveFromCurrentPlaylist);
            RefreshLibraryCommand = new Command(async () => await LoadMediaItemsAsync());
            TogglePlaylistEditorCommand = new Command(() => ShowPlaylistEditor = !ShowPlaylistEditor);
            SelectTabCommand = new Command<object>(param => 
            {
                if (param != null && int.TryParse(param.ToString(), out int index))
                {
                    SelectedTabIndex = index;
                }
            });

            Task.Run(async () => await InitializeAsync());
        }

        private async Task InitializeAsync()
        {
            await LoadMediaItemsAsync();
            await LoadPlaylistsAsync();
        }

        private async Task LoadMediaItemsAsync()
        {
            try
            {
                IsBusy = true;

                List<MediaItem> items;

                if (FilterType == "audio")
                {
                    items = await _mediaPlayerService.GetMediaItemsByTypeAsync("audio");
                }
                else if (FilterType == "video")
                {
                    items = await _mediaPlayerService.GetMediaItemsByTypeAsync("video");
                }
                else
                {
                    items = await _mediaPlayerService.GetMediaItemsAsync();
                }

                MediaLibrary.Clear();
                foreach (var item in items)
                {
                    MediaLibrary.Add(item);
                }

                StatusMessage = $"Loaded {items.Count} media items";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading media: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadPlaylistsAsync()
        {
            try
            {
                var playlists = await _mediaPlayerService.GetPlaylistsAsync();

                Playlists.Clear();
                foreach (var playlist in playlists)
                {
                    Playlists.Add(playlist);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading playlists: {ex.Message}";
            }
        }

        private async Task PlayMediaItemAsync(MediaItem item)
        {
            try
            {
                CurrentMediaItem = item;
                Duration = item.Duration.TotalSeconds;
                CurrentPosition = 0;

                // Start playback
                IsPlaying = true;
                IsPaused = false;
                StatusMessage = $"Playing: {item.Title}";

                // Notify property changes for UI
                OnPropertyChanged(nameof(CurrentMediaTitle));
                OnPropertyChanged(nameof(CurrentMediaType));
                OnPropertyChanged(nameof(IsVideoPlaying));
                OnPropertyChanged(nameof(IsAudioPlaying));

                // If this item is part of current playlist, update index
                var index = CurrentPlaylistItems.ToList().FindIndex(i => i.Id == item.Id);
                if (index >= 0)
                {
                    _currentPlaylistIndex = index;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error playing media: {ex.Message}";
            }
        }

        private void Play()
        {
            if (CurrentMediaItem == null && CurrentPlaylistItems.Count > 0)
            {
                // Start playing first item in queue
                Task.Run(async () => await PlayMediaItemAsync(CurrentPlaylistItems[0]));
            }
            else if (IsPaused)
            {
                // Resume playback
                IsPaused = false;
                IsPlaying = true;
                StatusMessage = "Resumed";
            }
            else if (CurrentMediaItem != null)
            {
                // Restart current item
                CurrentPosition = 0;
                IsPlaying = true;
                IsPaused = false;
                StatusMessage = $"Playing: {CurrentMediaItem.Title}";
            }

            OnPropertyChanged(nameof(IsVideoPlaying));
            OnPropertyChanged(nameof(IsAudioPlaying));
        }

        private void Pause()
        {
            if (IsPlaying)
            {
                IsPlaying = false;
                IsPaused = true;
                StatusMessage = "Paused";

                OnPropertyChanged(nameof(IsVideoPlaying));
                OnPropertyChanged(nameof(IsAudioPlaying));
            }
        }

        private void Stop()
        {
            IsPlaying = false;
            IsPaused = false;
            CurrentPosition = 0;
            StatusMessage = "Stopped";

            OnPropertyChanged(nameof(IsVideoPlaying));
            OnPropertyChanged(nameof(IsAudioPlaying));
        }

        private void PlayNext()
        {
            if (CurrentPlaylistItems.Count == 0)
                return;

            if (IsShuffleEnabled)
            {
                var random = new Random();
                _currentPlaylistIndex = random.Next(0, CurrentPlaylistItems.Count);
            }
            else
            {
                _currentPlaylistIndex++;

                if (_currentPlaylistIndex >= CurrentPlaylistItems.Count)
                {
                    if (IsRepeatEnabled)
                    {
                        _currentPlaylistIndex = 0;
                    }
                    else
                    {
                        Stop();
                        return;
                    }
                }
            }

            var nextItem = CurrentPlaylistItems[_currentPlaylistIndex];
            Task.Run(async () => await PlayMediaItemAsync(nextItem));
        }

        private void PlayPrevious()
        {
            if (CurrentPlaylistItems.Count == 0)
                return;

            if (CurrentPosition > 3) // If more than 3 seconds in, restart current track
            {
                CurrentPosition = 0;
                return;
            }

            _currentPlaylistIndex--;

            if (_currentPlaylistIndex < 0)
            {
                if (IsRepeatEnabled)
                {
                    _currentPlaylistIndex = CurrentPlaylistItems.Count - 1;
                }
                else
                {
                    _currentPlaylistIndex = 0;
                    return;
                }
            }

            var previousItem = CurrentPlaylistItems[_currentPlaylistIndex];
            Task.Run(async () => await PlayMediaItemAsync(previousItem));
        }

        private void ToggleFullScreen()
        {
            IsFullScreen = !IsFullScreen;
            StatusMessage = IsFullScreen ? "Full screen mode" : "Normal mode";
        }

        private void ToggleRepeat()
        {
            IsRepeatEnabled = !IsRepeatEnabled;
            StatusMessage = IsRepeatEnabled ? "Repeat enabled" : "Repeat disabled";
        }

        private void ToggleShuffle()
        {
            IsShuffleEnabled = !IsShuffleEnabled;
            StatusMessage = IsShuffleEnabled ? "Shuffle enabled" : "Shuffle disabled";
        }

        private void AddToQueue(MediaItem item)
        {
            if (!CurrentPlaylistItems.Any(i => i.Id == item.Id))
            {
                CurrentPlaylistItems.Add(item);
                StatusMessage = $"Added to queue: {item.Title}";
            }
            else
            {
                StatusMessage = "Already in queue";
            }
        }

        private void AddToCurrentPlaylist(MediaItem item)
        {
            if (!CurrentPlaylistItems.Any(i => i.Id == item.Id))
            {
                CurrentPlaylistItems.Add(item);
                StatusMessage = $"Added: {item.Title}";
            }
        }

        private void RemoveFromCurrentPlaylist(MediaItem item)
        {
            CurrentPlaylistItems.Remove(item);
            StatusMessage = $"Removed: {item.Title}";
        }

        private async Task CreatePlaylistAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(PlaylistName))
                {
                    StatusMessage = "Please enter a playlist name";
                    return;
                }

                var playlist = new Playlist
                {
                    Name = PlaylistName.Trim(),
                    Description = PlaylistDescription.Trim(),
                    Type = "mixed"
                };

                await _mediaPlayerService.CreatePlaylistAsync(playlist);

                // Add current items to the playlist
                foreach (var item in CurrentPlaylistItems)
                {
                    await _mediaPlayerService.AddItemToPlaylistAsync(playlist.Id, item.Id);
                }

                await LoadPlaylistsAsync();

                PlaylistName = string.Empty;
                PlaylistDescription = string.Empty;
                StatusMessage = "Playlist created successfully";
                ShowPlaylistEditor = false;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error creating playlist: {ex.Message}";
            }
        }

        private async Task LoadPlaylistAsync(Playlist playlist)
        {
            try
            {
                IsBusy = true;
                CurrentPlaylist = playlist;

                var items = await _mediaPlayerService.GetPlaylistItemsAsync(playlist.Id);

                CurrentPlaylistItems.Clear();
                foreach (var item in items)
                {
                    CurrentPlaylistItems.Add(item);
                }

                _currentPlaylistIndex = -1;
                StatusMessage = $"Loaded playlist: {playlist.Name}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading playlist: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SaveCurrentPlaylistAsync()
        {
            try
            {
                if (CurrentPlaylist == null)
                {
                    StatusMessage = "No playlist selected";
                    return;
                }

                // Update playlist
                await _mediaPlayerService.UpdatePlaylistAsync(CurrentPlaylist);

                StatusMessage = "Playlist saved";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving playlist: {ex.Message}";
            }
        }

        private async Task DeletePlaylistAsync(Playlist playlist)
        {
            try
            {
                bool confirm = await Shell.Current.DisplayAlert(
                    "Delete Playlist",
                    $"Are you sure you want to delete '{playlist.Name}'?",
                    "Yes",
                    "No");

                if (confirm)
                {
                    await _mediaPlayerService.DeletePlaylistAsync(playlist.Id);
                    Playlists.Remove(playlist);

                    if (CurrentPlaylist?.Id == playlist.Id)
                    {
                        CurrentPlaylist = null;
                        CurrentPlaylistItems.Clear();
                    }

                    StatusMessage = "Playlist deleted";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error deleting playlist: {ex.Message}";
            }
        }

        // This method should be called when media naturally ends to auto-advance
        public void OnMediaEnded()
        {
            // Auto-advance to next track for seamless playback
            PlayNext();
        }

        // This method should be called to update position during playback
        public void UpdatePosition(double position)
        {
            CurrentPosition = position;
            OnPropertyChanged(nameof(CurrentPositionText));
        }
    }
}
