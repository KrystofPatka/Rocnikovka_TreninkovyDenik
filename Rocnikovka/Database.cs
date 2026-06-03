using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rocnikovka;

public class Database
{
    private readonly SQLiteAsyncConnection _database;
    private bool _initialized = false;

    public Database(string dbPath)
    {
        _database = new SQLiteAsyncConnection(dbPath);
    }

    private async Task InitAsync()
    {
        if (_initialized) return;

        await _database.CreateTableAsync<Trenink>();
        await _database.CreateTableAsync<Cvik>();
        _initialized = true;
    }

    public async Task<List<Trenink>> GetTreninkyAsync()
    {
        await InitAsync();
        return await _database.Table<Trenink>().OrderByDescending(x => x.Datum).ToListAsync();
    }

    public async Task<int> SaveTreninkAsync(Trenink trenink)
    {
        await InitAsync();
        return await _database.InsertAsync(trenink);
    }

    public async Task<int> DeleteTreninkAsync(Trenink trenink)
    {
        await InitAsync();
        return await _database.DeleteAsync(trenink);
    }

    public async Task<List<Cvik>> GetCvikyAsync()
    {
        await InitAsync();
        return await _database.Table<Cvik>().ToListAsync();
    }

    public async Task<int> SaveCvikAsync(Cvik cvik)
    {
        await InitAsync();
        return await _database.InsertAsync(cvik);
    }
}