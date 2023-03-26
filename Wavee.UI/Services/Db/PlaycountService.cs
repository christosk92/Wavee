using Dapper;
using Microsoft.Data.Sqlite;
using Wavee.UI.Interfaces.Services;

namespace Wavee.UI.Services.Db;

public class PlaycountService : IPlaycountService
{
    private readonly string _connectionString;

    public PlaycountService(IAppDataProvider appDataProvider)
    {
        var path = Path.Combine(appDataProvider.GetAppDataRoot(), "localdata.db");
        _connectionString = $"Data Source={path}";
        InitializeDatabase();
    }


    public async Task IncrementPlayCount(string id, DateTime startedAt, TimeSpan duration,
        CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);

        const string insert = @"
            INSERT INTO Playcount (TrackId, DatePlayed, Duration)
            VALUES (@TrackId, @DatePlayed, @Duration);";

        var parameters = new
        {
            TrackId = id,
            DatePlayed = startedAt.ToString("o"),
            Duration = duration.ToString()
        };

        await connection.ExecuteAsync(insert, parameters);
    }

    public async Task<IEnumerable<DateTime>> GetPlayDates(string id, CancellationToken ct = default)
    {
        //order by date played (desc)
        const string select = @"
            SELECT DatePlayed
            FROM Playcount
            WHERE TrackId = @TrackId
            ORDER BY DatePlayed DESC;";
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        var parameters = new
        {
            TrackId = id
        };
        var dates = await connection.QueryAsync<DateTime>(select, parameters);
        return dates;
    }

    public async Task<IReadOnlyDictionary<string, (int Playcounts, DateTime LastPlayed)>> GetPlaycounts(
        IList<string> tracks,
        CancellationToken ct = default)
    {
        //group by track id and count the number of rows and the max date played
        const string select = @"
            SELECT TrackId, COUNT(*) AS Playcount, MAX(DatePlayed) AS LastPlayed
            FROM Playcount
            WHERE TrackId IN @TrackIds
            GROUP BY TrackId;";

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        var parameters = new
        {
            TrackIds = tracks
        };
        var playcounts = await connection.QueryAsync<(string TrackId, int Playcount, DateTime LastPlayed)>(select,
            parameters);

        return tracks.ToDictionary(t => t,
            t =>
            {
                var data = playcounts.FirstOrDefault(p => p.TrackId == t);
                return (data.Playcount, data.LastPlayed);
            });
    }

    private void InitializeDatabase()
    {
        // Create the database if it doesn't exist
        //simple table with incrementing id, track id, and date
        const string createTable = @"
            CREATE TABLE IF NOT EXISTS Playcount (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TrackId TEXT NOT NULL,
                DatePlayed TEXT NOT NULL,
                Duration TEXT NOT NULL
            );";
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var createTableCommand = new SqliteCommand(createTable, connection);
        createTableCommand.ExecuteNonQuery();
    }
}