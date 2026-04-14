using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using VinhKhanhTour.Models;
using VinhKhanhTour.Services;
using VinhKhanhTour.Utilities;
using System.Diagnostics;

namespace VinhKhanhTour.Pages;

[QueryProperty(nameof(SelectedPoiId), "poiId")]
public partial class MapPage : ContentPage
{
    private readonly NarrationEngine _narrationEngine;
    private readonly ApiService _apiService;
    private readonly Data.PoiRepository _poiRepository;

    private List<Poi> _poisFromApi = new();
    private bool _isTrackingLocation = false;
    private bool _isMapLoaded = false;

    private int _selectedPoiId = 0;
    private int _playingPoiId = 0;

    // ── Simulation ───────────────────────────────────────────────────────────────────
    private bool _isSimulating = false;
    private double _simLat = 0;
    private double _simLon = 0;
    // 10 mét tính bằng độ kinh / vĩ
    private const double StepLat = 10.0 / 111_000.0;
    // cos(10.76°) ≈ 0.9825  (vĩ độ khu vực TP.HCM)
    private const double StepLon = 10.0 / (111_000.0 * 0.9825);
    // ── Fix 2: Chặn race condition khi bấm D-Pad nhanh ────────────────────────────
    private bool _isHandlingLocation = false;
    // ── Fix 6: Theo dõi POI đang xem để Prev/Next điều hướng ────────────────────
    private int _currentPoiIndex = 0;
    // ── Fix Bug#3: Lưu reference timer để tránh GC và có thể Stop() ──────────────
    private IDispatcherTimer? _locationTimer;
    // ───────────────────────────────────────────────────────────────────

#if ANDROID || IOS || MACCATALYST
    private Microsoft.Maui.Controls.Maps.Map VinhKhanhMap;
    private Dictionary<int, Microsoft.Maui.Controls.Maps.Circle> _poiCircles = new();
    private Microsoft.Maui.Controls.Maps.Circle? _simMarker; // Chấm trắng thể hiện vị trí giả lập
#endif

    public int SelectedPoiId
    {
        get => _selectedPoiId;
        set => _selectedPoiId = value;
    }

    public MapPage(NarrationEngine narrationEngine, ApiService apiService, Data.PoiRepository poiRepository)
    {
        InitializeComponent();
        _narrationEngine = narrationEngine;
        _apiService = apiService;
        _poiRepository = poiRepository;
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        if (!_isMapLoaded)
        {
            await LoadPoisToMapAsync();
            _isMapLoaded = true;
        }

        await StartLocationTracking();
    }

    private async Task LoadPoisToMapAsync()
    {
#if ANDROID || IOS || MACCATALYST
        VinhKhanhMap = new Microsoft.Maui.Controls.Maps.Map
        {
            IsShowingUser = true,
            MapType = MapType.Street
        };

        MapContainer.Children.Insert(0, VinhKhanhMap);

        // 🔥 LOAD từ CMS API (không dùng local DB vì đã trống)
        _poisFromApi = await _apiService.GetPoisAsync();

        foreach (var poi in _poisFromApi)
        {
            var pin = new Pin
            {
                Label    = string.IsNullOrWhiteSpace(poi.Name) ? "Điểm đến" : poi.Name,
                Address  = "Vĩnh Khánh, Quận 4", // Bắt buộc có Address để tránh crash khi click
                Location = new Location(poi.Latitude, poi.Longitude),
                Type     = PinType.Place
            };

            VinhKhanhMap.Pins.Add(pin);

            double safeRadius = poi.Radius > 0 ? poi.Radius : 30;
            var circle = new Microsoft.Maui.Controls.Maps.Circle
            {
                Center      = new Location(poi.Latitude, poi.Longitude),
                Radius      = Distance.FromMeters(safeRadius),
                StrokeColor = Colors.Gray,
                StrokeWidth = 2,
                FillColor   = Color.FromRgba(128, 128, 128, 50)
            };

            VinhKhanhMap.MapElements.Add(circle);
            _poiCircles[poi.Id] = circle;
        }
#endif
    }

    private async Task StartLocationTracking()
    {
        if (_isTrackingLocation) return;

        var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

        if (status == PermissionStatus.Granted)
        {
            _isTrackingLocation = true;

            // Bug#3 Fix: Lưu vào field để tránh bị GC và có thể dừng đúng cách
            _locationTimer = Dispatcher.CreateTimer();
            _locationTimer.Interval = TimeSpan.FromSeconds(5);
            _locationTimer.Tick += async (s, e) => await CheckLocation();
            _locationTimer.Start();
        }
    }

    // Bug#3 Fix: Dừng timer khi rời trang, tránh GPS polling chạy ngầm
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _locationTimer?.Stop();
        _isTrackingLocation = false; // Cho phép restart khi quay lại
    }

    private async Task CheckLocation()
    {
        // Bỏ qua GPS thật khi đang giả lập
        if (_isSimulating) return;

        try
        {
            var location = await Geolocation.Default.GetLocationAsync();
            if (location == null) return;
            HandleLocation(location);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    private async void HandleLocation(Location userLocation)
    {
        // Fix 2: Bỏ qua nếu đang xử lý từ lệnh gọi trước -> tránh race condition
        if (_isHandlingLocation) return;
        _isHandlingLocation = true;

        try
        {
            if (_poisFromApi.Count == 0) return;

            Poi? nearest = null;
            double minDistance = double.MaxValue;

            // Tìm POI gần nhất trong bán kính
            foreach (var poi in _poisFromApi)
            {
                double distance = LocationHelper.CalculateDistanceInMeters(
                    userLocation.Latitude, userLocation.Longitude,
                    poi.Latitude, poi.Longitude);

                if (distance <= poi.Radius && distance < minDistance)
                {
                    nearest = poi;
                    minDistance = distance;
                }
            }

            if (nearest != null)
            {
                // Fix 4: Cập nhật label "Bạn đang ở gần" và khoảng cách
                PoiNameLabel.Text = nearest.DisplayName;
                DistanceLabel.Text = $"Khoảng cách: {minDistance:F0} m";

                if (_playingPoiId != nearest.Id)
                {
                    _playingPoiId = nearest.Id;
                    _currentPoiIndex = _poisFromApi.IndexOf(nearest);
                    await _narrationEngine.PlayPoiNarrationAsync(nearest);
                }
            }
            else
            {
                // Fix 4: Khi ngoài vùng, hiển thị POI gần nhất cùng khoảng cách
                Poi? nearestOutside = null;
                double nearestDist = double.MaxValue;
                foreach (var poi in _poisFromApi)
                {
                    double d = LocationHelper.CalculateDistanceInMeters(
                        userLocation.Latitude, userLocation.Longitude,
                        poi.Latitude, poi.Longitude);
                    if (d < nearestDist) { nearestOutside = poi; nearestDist = d; }
                }

                PoiNameLabel.Text = nearestOutside?.DisplayName
                    ?? Services.LocalizationResourceManager.Instance["Chọn một điểm trên map"];
                DistanceLabel.Text = nearestOutside != null
                    ? $"{Services.LocalizationResourceManager.Instance["Cách quán gần nhất"]}: {nearestDist:F0} m"
                    : "Khoảng cách: -- m";

                _playingPoiId = 0;
            }
        }
        finally
        {
            _isHandlingLocation = false;
        }
    }

    // ── Simulation Handlers ─────────────────────────────────────────────

    private async void OnSimToggleClicked(object sender, EventArgs e)
    {
        _isSimulating = !_isSimulating;

        if (_isSimulating)
        {
            // Lấy vị trí GPS hiện tại làm điểm khởi đầu
            try
            {
                Location? location = await Geolocation.Default.GetLastKnownLocationAsync();

                if (location == null)
                {
                    var req = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(3));
                    location = await Geolocation.Default.GetLocationAsync(req);
                }

                if (location != null)
                {
                    _simLat = location.Latitude;
                    _simLon = location.Longitude;
                }
                else
                {
                    // Fallback: khu vực đường Vĩnh Khánh, Quận 4
                    _simLat = 10.7553;
                    _simLon = 106.7022;
                }
            }
            catch
            {
                _simLat = 10.7553;
                _simLon = 106.7022;
            }

            SimToggleButton.Text = "🛑 Tắt Giả lập";
            SimToggleButton.BackgroundColor = Color.FromArgb("#E63946");
            DPadGrid.IsVisible = true;
            SimCoordLabel.IsVisible = true;
            UpdateSimCoordLabel();

#if ANDROID || IOS || MACCATALYST
            VinhKhanhMap?.MoveToRegion(MapSpan.FromCenterAndRadius(
                new Location(_simLat, _simLon),
                Distance.FromMeters(300)));
            UpdateSimMarker(); // Hiển thị chấm trắng tại vị trí khởi đầu
#endif
        }
        else
        {
            SimToggleButton.Text = "🎮 Giả lập";
            SimToggleButton.BackgroundColor = Color.FromArgb("#6C757D");
            DPadGrid.IsVisible = false;
            SimCoordLabel.IsVisible = false;
            _playingPoiId = 0; // Reset để GPS thật có thể kích hoạt lại
            _narrationEngine.CancelCurrentNarration();

#if ANDROID || IOS || MACCATALYST
            // Xóa chấm trắng giả lập khi tắt
            if (_simMarker != null)
            {
                VinhKhanhMap?.MapElements.Remove(_simMarker);
                _simMarker = null;
            }
#endif
        }
    }

    private void MoveSimulation(double dLat, double dLon)
    {
        if (!_isSimulating) return;

        _simLat += dLat;
        _simLon += dLon;
        UpdateSimCoordLabel();

        var fakeLocation = new Location(_simLat, _simLon);
        HandleLocation(fakeLocation);

#if ANDROID || IOS || MACCATALYST
        VinhKhanhMap?.MoveToRegion(MapSpan.FromCenterAndRadius(
            new Location(_simLat, _simLon),
            Distance.FromMeters(300)));
        UpdateSimMarker(); // Di chuyển chấm trắng theo D-Pad
#endif
    }

    /// <summary>
    /// Vẽ (hoặc cập nhật) chấm trắng thể hiện vị trí giả lập trên bản đồ.
    /// </summary>
    private void UpdateSimMarker()
    {
#if ANDROID || IOS || MACCATALYST
        if (VinhKhanhMap == null) return;

        // Xóa chấm cũ trước khi vẽ lại
        if (_simMarker != null)
            VinhKhanhMap.MapElements.Remove(_simMarker);

        _simMarker = new Microsoft.Maui.Controls.Maps.Circle
        {
            Center      = new Location(_simLat, _simLon),
            Radius      = Distance.FromMeters(12),          // kích thước chấm ~12m
            StrokeColor = Color.FromArgb("#FF6B35"),        // Viền màu cam (Primary)
            StrokeWidth = 3,
            FillColor   = Colors.White                      // Nhân trắng, phân biệt với chấm xanh GPS
        };
        VinhKhanhMap.MapElements.Add(_simMarker);
#endif
    }

    private void UpdateSimCoordLabel()
    {
        SimCoordLabel.Text = $"📍 {_simLat:F5}, {_simLon:F5}";
    }

    private void OnSimNorthClicked(object sender, EventArgs e) => MoveSimulation(StepLat, 0);
    private void OnSimSouthClicked(object sender, EventArgs e) => MoveSimulation(-StepLat, 0);
    private void OnSimEastClicked(object sender, EventArgs e)  => MoveSimulation(0, StepLon);
    private void OnSimWestClicked(object sender, EventArgs e)  => MoveSimulation(0, -StepLon);

    // ── Fix 6: Prev/Next POI navigation ───────────────────────────────

    private async void OnPrevPoiClicked(object sender, EventArgs e)
    {
        if (_poisFromApi.Count == 0) return;
        _currentPoiIndex = (_currentPoiIndex - 1 + _poisFromApi.Count) % _poisFromApi.Count;
        await NavigateToPoiAtIndexAsync(_currentPoiIndex);
    }

    private async void OnNextPoiClicked(object sender, EventArgs e)
    {
        if (_poisFromApi.Count == 0) return;
        _currentPoiIndex = (_currentPoiIndex + 1) % _poisFromApi.Count;
        await NavigateToPoiAtIndexAsync(_currentPoiIndex);
    }

    private async Task NavigateToPoiAtIndexAsync(int index)
    {
        var poi = _poisFromApi[index];

        // Cập nhật labels ngay lập tức
        PoiNameLabel.Text = poi.DisplayName;
        DistanceLabel.Text = "0 m";

        // Trong simulation: teleport đến tọa độ POI
        if (_isSimulating)
        {
            _simLat = poi.Latitude;
            _simLon = poi.Longitude;
            UpdateSimCoordLabel();
            _playingPoiId = 0; // Cho phép audio phát lại POI này

#if ANDROID || IOS || MACCATALYST
            VinhKhanhMap?.MoveToRegion(MapSpan.FromCenterAndRadius(
                new Location(_simLat, _simLon),
                Distance.FromMeters(200)));
            UpdateSimMarker(); // Teleport chấm trắng đến POI
#endif
        }
        else
        {
            // Không ở simulation: chỉ pan bản đồ đến POI
#if ANDROID || IOS || MACCATALYST
            VinhKhanhMap?.MoveToRegion(MapSpan.FromCenterAndRadius(
                new Location(poi.Latitude, poi.Longitude),
                Distance.FromMeters(300)));
#endif
        }

        // Phát narration thủ công
        _playingPoiId = poi.Id;
        await _narrationEngine.PlayPoiNarrationAsync(poi, isManual: true);
    }

    // ───────────────────────────────────────────────────────────────────

    private async void OnMyLocationClicked(object sender, EventArgs e)
    {
#if ANDROID || IOS || MACCATALYST
        try
        {
            var location = await Geolocation.Default.GetLocationAsync();
            if (location == null || VinhKhanhMap == null) return;

            VinhKhanhMap.MoveToRegion(MapSpan.FromCenterAndRadius(
                new Location(location.Latitude, location.Longitude),
                Distance.FromMeters(500)));
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
#endif
    }

    private void OnZoomInClicked(object sender, EventArgs e)
    {
#if ANDROID || IOS || MACCATALYST
        try
        {
            if (VinhKhanhMap?.VisibleRegion == null) return;
            var center = VinhKhanhMap.VisibleRegion.Center;
            VinhKhanhMap.MoveToRegion(MapSpan.FromCenterAndRadius(center, Distance.FromMeters(500)));
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
#endif
    }

    private void OnZoomOutClicked(object sender, EventArgs e)
    {
#if ANDROID || IOS || MACCATALYST
        try
        {
            if (VinhKhanhMap?.VisibleRegion == null) return;
            var center = VinhKhanhMap.VisibleRegion.Center;
            VinhKhanhMap.MoveToRegion(MapSpan.FromCenterAndRadius(center, Distance.FromMeters(2000)));
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
#endif
    }

    private void OnPlayPauseClicked(object sender, EventArgs e)
    {
        _narrationEngine.CancelCurrentNarration();
    }
}