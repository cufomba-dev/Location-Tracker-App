using MauiApp1.Models;
using Microsoft.Maui.Devices.Sensors;

namespace MauiApp1;

public class LocationService : ILocationService
{
    public event EventHandler<LocationSample>? LocationSampled;

    PeriodicTimer? _timer;
    CancellationTokenSource? _cts;
    public bool IsRunning => _timer is not null;

    public async Task StartAsync(TimeSpan interval)
    {
        if (IsRunning) return;

        var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
            throw new PermissionException("Location permission not granted.");

        _cts = new();
        _timer = new(interval);

        try
        {
            while (await _timer.WaitForNextTickAsync(_cts.Token))
            {
                var loc = await Geolocation.GetLocationAsync(new GeolocationRequest(
                    GeolocationAccuracy.Best, TimeSpan.FromSeconds(10)));
                if (loc is null) continue;

                LocationSampled?.Invoke(this, new Models.LocationSample
                {
                    Latitude = loc.Latitude,
                    Longitude = loc.Longitude,
                    TimestampUtc = DateTime.UtcNow
                });
            }
        }
        catch (OperationCanceledException) { }
    }

    public Task StopAsync()
    {
        _cts?.Cancel();
        _timer = null;
        return Task.CompletedTask;
    }
}
