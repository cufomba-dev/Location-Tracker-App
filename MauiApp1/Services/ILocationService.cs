using MauiApp1.Models;

namespace MauiApp1;

public interface ILocationService
{
    event EventHandler<LocationSample>? LocationSampled;
    bool IsRunning { get; }
    Task StartAsync(TimeSpan interval);
    Task StopAsync();
}
