using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Wavee.Repositories;

/// <summary>
/// Implements a generic cache repository using SQLite for persistent storage.
/// </summary>
/// <typeparam name="TKey">The type of the cache key.</typeparam>
public class SqliteCacheRepository<TKey> : ICacheRepository<TKey>
{
    private readonly string _cacheConnectionString;
    private readonly ILogger<SqliteCacheRepository<TKey>> _logger;

    public SqliteCacheRepository(string cacheDbPath, ILogger<SqliteCacheRepository<TKey>> logger)
    {
        if (string.IsNullOrWhiteSpace(cacheDbPath))
            throw new ArgumentException("Cache database path must be a valid file path.", nameof(cacheDbPath));

        var finalPath = Path.Combine(cacheDbPath, "data.db");
        _cacheConnectionString = $"Data Source={finalPath}";
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        try
        {
            using var connection = new SqliteConnection(_cacheConnectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Cache (
                        Key TEXT NOT NULL PRIMARY KEY,
                        ETag TEXT,
                        Data BLOB NOT NULL
                    );
                ";
            command.ExecuteNonQuery();

            _logger.LogInformation("Cache database initialized successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize the cache database.");
            throw;
        }
    }

    // Individual GetAsync
    public async Task<CacheEntry?> GetAsync(TKey key)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        var result = await GetInternalAsync(key);
        return result;
    }

    // Individual SetAsync
    public async Task SetAsync(TKey key, CacheEntry entry)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (entry == null) throw new ArgumentNullException(nameof(entry));

        await SetAsync(new[] { new KeyValuePair<TKey, CacheEntry>(key, entry) });
    }

    public async Task<CacheEntry> GetInternalAsync(TKey id)
    {
        var parameters = new Dictionary<string, object>
        {
            { "$key", id?.ToString() }
        };
        const string sql = "SELECT Key, Data, ETag FROM Cache WHERE Key = $key";

        await using var connection = new SqliteConnection(_cacheConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        foreach (var kvp in parameters)
        {
            var param = command.CreateParameter();
            param.ParameterName = kvp.Key;
            param.Value = kvp.Value;
            command.Parameters.Add(param);
        }

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var keyString = reader.GetString(0);
            var data = (byte[])reader["Data"];
            var etag = reader.IsDBNull(2) ? null : reader.GetString(2);

            var key = (TKey)Convert.ChangeType(keyString, typeof(TKey));
            return new CacheEntry { Data = data, Etag = etag };
        }


        return null;
    }

    // Batch GetAsync
    public async Task<Dictionary<TKey, CacheEntry>> GetAsync(IEnumerable<TKey> keys)
    {
        var results = new Dictionary<TKey, CacheEntry>();
        var keyList = keys.ToList();
        if (!keyList.Any()) return results;

        var parameters = keyList.Select((k, i) => $"$key{i}").ToArray();
        var sql = $"SELECT Key, Data, ETag FROM Cache WHERE Key IN ({string.Join(",", parameters)})";

        using var connection = new SqliteConnection(_cacheConnectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = sql;

        for (int i = 0; i < keyList.Count; i++)
        {
            command.Parameters.AddWithValue(parameters[i], keyList[i]?.ToString());
        }

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var keyString = reader.GetString(0);
            var data = (byte[])reader["Data"];
            var etag = reader.IsDBNull(2) ? null : reader.GetString(2);

            var key = (TKey)Convert.ChangeType(keyString, typeof(TKey));
            results.Add(key, new CacheEntry { Data = data, Etag = etag });
        }

        return results;
    }

    // Batch SetAsync
    public async Task SetAsync(IEnumerable<KeyValuePair<TKey, CacheEntry>> entries)
    {
        if (entries == null) throw new ArgumentNullException(nameof(entries));
        var entryList = entries.ToList();
        if (!entryList.Any()) return;

        using var connection = new SqliteConnection(_cacheConnectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();

        var command = connection.CreateCommand();
        command.CommandText = @"
                INSERT INTO Cache (Key, ETag, Data)
                VALUES ($key, $etag, $data)
                ON CONFLICT(Key) DO UPDATE SET
                    ETag = excluded.ETag,
                    Data = excluded.Data;
            ";

        var keyParam = command.CreateParameter();
        keyParam.ParameterName = "$key";
        command.Parameters.Add(keyParam);

        var etagParam = command.CreateParameter();
        etagParam.ParameterName = "$etag";
        command.Parameters.Add(etagParam);

        var dataParam = command.CreateParameter();
        dataParam.ParameterName = "$data";
        command.Parameters.Add(dataParam);

        foreach (var entry in entryList)
        {
            keyParam.Value = entry.Key?.ToString();
            etagParam.Value = entry.Value.Etag ?? (object)DBNull.Value;
            dataParam.Value = entry.Value.Data;

            await command.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
    }

    public async Task DeleteAsync(TKey key)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        
        await using var connection = new SqliteConnection(_cacheConnectionString);
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Cache WHERE Key = $key";
        command.Parameters.AddWithValue("$key", key?.ToString());
        
        await command.ExecuteNonQueryAsync();
    }
}