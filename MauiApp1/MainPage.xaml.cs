using Microsoft.Maui.Controls.Maps;    // Circle, Polyline
using Microsoft.Maui.Maps;             // MapSpan
using Microsoft.Maui.Devices.Sensors;  // Location, CalculateDistance

namespace MauiApp1;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _vm;

    // Route line
    private readonly Polyline _path = new()
    {
        StrokeWidth = 6,
        StrokeColor = Colors.Red.WithAlpha(0.8f)
    };

    // Heat overlays and dots
    private readonly List<Circle> _heatCircles = new();
    private readonly List<Circle> _dots = new();

    public MainPage(MainViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;

        Appearing += async (_, __) =>
        {
            if (!Map.MapElements.Contains(_path))
                Map.MapElements.Add(_path);

            await _vm.LoadAsync();   // load samples from SQLite
            Redraw();

            _vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.MapSpan))
                    Map.MoveToRegion(_vm.MapSpan);
            };

            _vm.Samples.CollectionChanged += (_, __) => Redraw();
        };
    }

	// add near other fields
private readonly bool _isIos = DeviceInfo.Platform == DevicePlatform.iOS;
private readonly bool _isAndroid = DeviceInfo.Platform == DevicePlatform.Android;

	private void Redraw()
	{
		// 1) Polyline route
		_path.Geopath.Clear();
		foreach (var s in _vm.Samples)
			_path.Geopath.Add(new Location(s.Latitude, s.Longitude));

		// 2) Clear previous overlays
		foreach (var c in _heatCircles) Map.MapElements.Remove(c);
		foreach (var d in _dots) Map.MapElements.Remove(d);
		_heatCircles.Clear();
		_dots.Clear();

		// ===== iOS style (left screenshot): red ribbon heat + blue current dot =====
		if (_isIos)
		{
			// make the polyline a bit thicker
			_path.StrokeWidth = 8;

			// narrow red ribbon: many small overlapping red disks right on the path
			// (no big halo; this keeps the effect tight like the screenshot)
			const int ribbonCoreM = 18;  // inner hot spot
			const int ribbonOuterM = 28;  // soft edge

			foreach (var s in _vm.Samples)
			{
				var loc = new Location(s.Latitude, s.Longitude);

				var core = new Circle
				{
					Center = loc,
					Radius = Distance.FromMeters(ribbonCoreM),
					StrokeColor = Colors.Transparent,
					FillColor = Colors.Red.WithAlpha(0.55f)   // darker core
				};
				var edge = new Circle
				{
					Center = loc,
					Radius = Distance.FromMeters(ribbonOuterM),
					StrokeColor = Colors.Transparent,
					FillColor = Colors.Red.WithAlpha(0.22f)   // soft edge
				};

				_heatCircles.Add(edge); Map.MapElements.Add(edge);
				_heatCircles.Add(core); Map.MapElements.Add(core);
			}

			// small blue sample dots (subtle; the red ribbon is the star on iOS)
			const int iosDotM = 8;
			foreach (var s in _vm.Samples)
			{
				var dot = new Circle
				{
					Center = new Location(s.Latitude, s.Longitude),
					Radius = Distance.FromMeters(iosDotM),
					StrokeColor = Colors.Transparent,
					FillColor = Color.FromArgb("#0A84FF")     // iOS system blue
				};
				_dots.Add(dot);
				Map.MapElements.Add(dot);
			}
		}

		// ===== Android style (right screenshot): only blue dots =====
		if (_isAndroid)
		{
			// thin route line (or comment the next 2 lines to hide the polyline entirely)
			_path.StrokeWidth = 5;

			// bigger, solid blue dots (no white ring; matches the screenshot)
			const int androidDotM = 14;
			var blue = Color.FromArgb("#1E88E5");  // material-ish blue

			foreach (var s in _vm.Samples)
			{
				var dot = new Circle
				{
					Center = new Location(s.Latitude, s.Longitude),
					Radius = Distance.FromMeters(androidDotM),
					StrokeColor = Colors.Transparent,
					FillColor = blue
				};
				_dots.Add(dot);
				Map.MapElements.Add(dot);
			}
		}

		// 3) Auto-fit camera to the trail (with a floor so we don’t zoom too far)
		if (_vm.Samples.Count > 0)
		{
			double minLat = double.MaxValue, maxLat = double.MinValue;
			double minLon = double.MaxValue, maxLon = double.MinValue;

			foreach (var s in _vm.Samples)
			{
				minLat = Math.Min(minLat, s.Latitude);
				maxLat = Math.Max(maxLat, s.Latitude);
				minLon = Math.Min(minLon, s.Longitude);
				maxLon = Math.Max(maxLon, s.Longitude);
			}

			var center = new Location((minLat + maxLat) / 2.0, (minLon + maxLon) / 2.0);
			var ne = new Location(maxLat, maxLon);
			var sw = new Location(minLat, minLon);

			var r1 = Location.CalculateDistance(center, ne, DistanceUnits.Kilometers);
			var r2 = Location.CalculateDistance(center, sw, DistanceUnits.Kilometers);
			var radiusKm = Math.Max(r1, r2) * 1.25;

			// keep at least ~1 km radius so the ribbon/dots read well
			radiusKm = Math.Max(radiusKm, 1.0);

			Map.MoveToRegion(MapSpan.FromCenterAndRadius(center, Distance.FromKilometers(radiusKm)));
		}
	}

}
