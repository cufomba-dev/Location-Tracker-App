using SQLite;
using MauiApp1.Models;

namespace MauiApp1;

public class LocationDb
{
    readonly SQLiteAsyncConnection _db;

    public LocationDb()
    {
        var path = Path.Combine(FileSystem.AppDataDirectory, "locations.db3");
        _db = new SQLiteAsyncConnection(path);
        _ = _db.CreateTableAsync<LocationSample>();
    }

    public Task<int> InsertAsync(LocationSample s) => _db.InsertAsync(s);
    public Task<List<LocationSample>> GetAllAsync() =>
        _db.Table<LocationSample>().OrderBy(x => x.TimestampUtc).ToListAsync();
    public Task<int> DeleteAllAsync() => _db.DeleteAllAsync<LocationSample>();
}
