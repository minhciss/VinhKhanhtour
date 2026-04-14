using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanhTour.Data;
using VinhKhanhTour.Services;
using VinhKhanhTour.Models;
using System.Collections.ObjectModel;
using Plugin.Maui.Audio;

namespace VinhKhanhTour.PageModels
{
    public partial class MainPageModel : ObservableObject
    {
        private readonly PoiRepository _poiRepository;
        private readonly Services.IErrorHandler _errorHandler;
        private readonly Services.NarrationEngine _narrationEngine;
        private List<Poi> _allPois = [];
        private bool _isDataLoaded = false;
        private readonly ApiService _apiService = new();
     

        [ObservableProperty]
        private List<Poi> _pois = [];

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private bool _isRefreshing;

        public MainPageModel(PoiRepository poiRepository, Services.IErrorHandler errorHandler, Services.NarrationEngine narrationEngine)
        {
            _poiRepository = poiRepository;
            _errorHandler = errorHandler;
            _narrationEngine = narrationEngine;

            Services.LocalizationResourceManager.Instance.PropertyChanged += (s, e) => 
            {
                // Thông báo từng POI cập nhật DisplayName/DisplayDescription ngay lập tức
                // mà không cần restart app → CollectionView re-render cells với ngôn ngữ mới
                foreach (var poi in _allPois)
                    poi.RefreshDisplayProperties();

                ApplyFilters();
            };

            // Lắng nghe GPS liên tục
            Geolocation.Default.LocationChanged += OnLocationChanged;
        }

        private void OnLocationChanged(object? sender, GeolocationLocationChangedEventArgs e)
        {
            var userLocation = e.Location;
            if (userLocation != null)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    foreach (var poi in _allPois)
                    {
                        poi.DistanceToUser = Utilities.LocationHelper.CalculateDistanceInMeters(
                            userLocation.Latitude, userLocation.Longitude,
                            poi.Latitude, poi.Longitude);
                    }
                });
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                IsBusy = true;

                // Chỉ lấy POI từ CMS API — không dùng local DB
                var data = await _apiService.GetPoisAsync();
                _allPois = data ?? new List<Poi>();

                ApplyFilters();
            }
            catch (Exception ex)
            {
                _errorHandler.HandleError(ex);
                _allPois = new List<Poi>();
                ApplyFilters();
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void Search(string query)
        {
            SearchText = query;
            ApplyFilters();
        }

        [RelayCommand]
        private void Filter(string category)
        {
            // Placeholder logic for category filtering
            // For now, let's just use it to show we can filter
            ApplyFilters(category);
        }

        private void ApplyFilters(string category = "Tất cả")
        {
            var filtered = _allPois.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(p => p.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || 
                                               p.DisplayDescription.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            if (category != "Tất cả" && category != "All")
            {
                // In a real app, POI would have a Category property. 
                // For this demo, we'll simulate it by checking keywords in description.
                if (category == "Ốc & Hải sản" || category == "Snails & Seafood")
                    filtered = filtered.Where(p => p.DisplayName.Contains("Ốc", StringComparison.OrdinalIgnoreCase) || 
                                                   p.DisplayDescription.Contains("Ốc", StringComparison.OrdinalIgnoreCase) || 
                                                   p.DisplayDescription.Contains("Hải sản", StringComparison.OrdinalIgnoreCase) ||
                                                   p.DisplayName.Contains("Snail", StringComparison.OrdinalIgnoreCase) ||
                                                   p.DisplayDescription.Contains("Seafood", StringComparison.OrdinalIgnoreCase));
                else if (category == "Đồ nướng" || category == "BBQ")
                    filtered = filtered.Where(p => p.DisplayDescription.Contains("nướng", StringComparison.OrdinalIgnoreCase) || 
                                                   p.DisplayDescription.Contains("grilled", StringComparison.OrdinalIgnoreCase) ||
                                                   p.DisplayDescription.Contains("BBQ", StringComparison.OrdinalIgnoreCase));
                else if (category == "Món nước" || category == "Noodle/Broth")
                    filtered = filtered.Where(p => p.DisplayDescription.Contains("bún", StringComparison.OrdinalIgnoreCase) || 
                                                   p.DisplayDescription.Contains("lẩu", StringComparison.OrdinalIgnoreCase) ||
                                                   p.DisplayDescription.Contains("noodle", StringComparison.OrdinalIgnoreCase) ||
                                                   p.DisplayDescription.Contains("hotpot", StringComparison.OrdinalIgnoreCase));
                else if (category == "Sushi")
                    filtered = filtered.Where(p => p.DisplayName.Contains("Sushi", StringComparison.OrdinalIgnoreCase));
            }

            Pois = filtered.ToList();
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            try
            {
                IsRefreshing = true;
                await LoadDataAsync();
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        private async Task AppearingAsync()
        {
            if (!_isDataLoaded)
            {
                await RefreshAsync();
                _isDataLoaded = true;
            }

            // Bắt đầu hoặc tiếp tục theo dõi vị trí khi trang hiển thị
            try
            {
                if (!Geolocation.Default.IsListeningForeground)
                {
                    var request = new GeolocationListeningRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5));
                    await Geolocation.Default.StartListeningForegroundAsync(request);
                }
            }
            catch (Exception ex)
            {
                _errorHandler.HandleError(ex);
            }
        }

        [RelayCommand]
        private async Task StartTourAsync()
        {
            await Shell.Current.GoToAsync("//map");
        }

        [ObservableProperty]
        private Poi? _selectedPoi;

        [ObservableProperty]
        private bool _isPoiDetailVisible;

        [RelayCommand]
        private void GoToPoi(Poi poi)
        {
            if (poi != null)
            {
                SelectedPoi = poi;
                IsPoiDetailVisible = true;
            }
        }

        [RelayCommand]
        private void ClosePoiDetail()
        {
            IsPoiDetailVisible = false;
        }

        [RelayCommand]
        private async Task NavigateToMapAsync()
        {
            if (SelectedPoi != null)
            {
                IsPoiDetailVisible = false;
                await Shell.Current.GoToAsync($"//map?poiId={SelectedPoi.Id}");
            }
        }

        [RelayCommand]
        private async Task PlayNarrationAsync()
        {
            if (SelectedPoi != null)
            {
                await _narrationEngine.PlayPoiNarrationAsync(SelectedPoi, isManual: true);
            }
        }
        private readonly IAudioManager _audioManager = AudioManager.Current;
        private IAudioPlayer? _player;

        [ObservableProperty]
        private bool _isPlaying;
    }
}