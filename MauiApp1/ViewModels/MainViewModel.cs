using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Maps;
using MauiApp1.Models;

namespace MauiApp1;

public partial class MainViewModel : ObservableObject
{
    readonly ILocationService _loc;
    readonly LocationDb _db;

    [ObservableProperty] bool isTracking;
    [ObservableProperty] string status = "Idle";
    [ObservableProperty] MapSpan mapSpan = MapSpan.FromCenterAndRadius(
        new Location(37.3349, -122.0090), Distance.FromKilometers(3));

    public ObservableCollection<LocationSample> Samples { get; } = new();

    public IRelayCommand StartCommand { get; }
    public IRelayCommand StopCommand { get; }
    public IRelayCommand ClearCommand { get; }
    public IRelayCommand RefreshCommand { get; }

    public MainViewModel(ILocationService loc, LocationDb db)
    {
        _loc = loc; _db = db;

        _loc.LocationSampled += async (_, s) =>
        {
            await _db.InsertAsync(s);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Samples.Add(s);
                Status = $"Samples: {Samples.Count}";
            });
        };

        StartCommand = new AsyncRelayCommand(StartAsync);
        StopCommand = new AsyncRelayCommand(StopAsync);
        ClearCommand = new AsyncRelayCommand(ClearAsync);
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
    }

    async Task StartAsync()
    {
        if (IsTracking) return;
        IsTracking = true; Status = "Tracking…";
        await _loc.StartAsync(TimeSpan.FromSeconds(5));
    }

    async Task StopAsync()
    {
        if (!IsTracking) return;
        await _loc.StopAsync();
        IsTracking = false; Status = "Stopped";
    }

    async Task ClearAsync()
    {
        await _db.DeleteAllAsync(); Samples.Clear(); Status = "Cleared";
    }

    public async Task LoadAsync()
    {
        Samples.Clear();
        foreach (var s in await _db.GetAllAsync()) Samples.Add(s);
        if (Samples.Count > 0)
        {
            var last = Samples[^1];
            MapSpan = MapSpan.FromCenterAndRadius(
                new Location(last.Latitude, last.Longitude),
                Distance.FromKilometers(2));
        }
        Status = $"Loaded {Samples.Count}";
    }
}
