// SqliteSpotifyPlaylistRepository.cs

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Wavee.Interfaces;
using Wavee.Models.Playlist;
using System.Text.Json;

namespace Wavee.Repositories;

/// <summary>
/// Implements the <see cref="ISpotifyPlaylistRepository"/> interface using SQLite for persistent storage.
/// </summary>
public class SqliteSpotifyPlaylistRepository : ISpotifyPlaylistRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteSpotifyPlaylistRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteSpotifyPlaylistRepository"/> class.
    /// </summary>
    /// <param name="dbPath">The path to the SQLite database file.</param>
    /// <param name="logger">The logger instance.</param>
    public SqliteSpotifyPlaylistRepository(string dbPath, ILogger<SqliteSpotifyPlaylistRepository> logger)
    {
        if (string.IsNullOrWhiteSpace(dbPath))
            throw new ArgumentException("Database path must be a valid file path.", nameof(dbPath));

        var finalPath = Path.Combine(dbPath, "playlists.db");
        _connectionString = $"Data Source={finalPath}";
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        InitializeDatabase();
    }

    /// <summary>
    /// Initializes the database by creating the necessary tables if they do not exist.
    /// </summary>
    private void InitializeDatabase()
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();

            // Create the CachedPlaylists table
            command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS CachedPlaylists (
                        Uri TEXT NOT NULL PRIMARY KEY,
                        Name TEXT NOT NULL,
                        Description TEXT,
                        ItemIndex INTEGER NOT NULL,
                        AddedAt TEXT NOT NULL,
                        RevisionId TEXT NOT NULL,
                        Tracks TEXT
                    );
                ";
            command.ExecuteNonQuery();

            _logger.LogInformation("Playlists database initialized successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize the playlists database.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IList<SpotifyCachedPlaylistItem>> GetCachedPlaylists()
    {
        var items = new List<SpotifyCachedPlaylistItem>();

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
                SELECT Uri, Name, Description, ItemIndex, AddedAt, RevisionId, Tracks
                FROM CachedPlaylists
                ORDER BY ItemIndex ASC;
            ";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var uri = reader.GetString(0);
            var name = reader.GetString(1);
            var description = reader.IsDBNull(2) ? null : reader.GetString(2);
            var index = reader.GetInt32(3);
            var addedAt = DateTimeOffset.Parse(reader.GetString(4));
            var revisionId = reader.GetString(5);
            var tracksJson = reader.IsDBNull(6) ? null : reader.GetString(6);

            var tracks = tracksJson != null
                ? JsonSerializer.Deserialize<List<SpotifyCachedPlaylistTrack>>(tracksJson,
                    SpotifyClient.DefaultJsonOptions)
                : new List<SpotifyCachedPlaylistTrack>();

            var item = new SpotifyCachedPlaylistItem
            {
                Uri = uri,
                Name = name,
                Description = description,
                Index = index,
                AddedAt = addedAt,
                RevisionId = revisionId,
                Tracks = tracks
            };

            items.Add(item);
        }

        _logger.LogInformation("Retrieved {Count} cached playlists.", items.Count);
        return items;
    }

    /// <inheritdoc />
    public async Task SaveCachedPlaylists(IList<SpotifyCachedPlaylistItem> playlists)
    {
        if (playlists == null)
            throw new ArgumentNullException(nameof(playlists));

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // Insert or update playlists
            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = @"
                    INSERT INTO CachedPlaylists (Uri, Name, Description, ItemIndex, AddedAt, RevisionId, Tracks)
                    VALUES ($uri, $name, $description, $itemIndex, $addedAt, $revisionId, $tracks)
                    ON CONFLICT(Uri) DO UPDATE SET
                        Name = excluded.Name,
                        Description = excluded.Description,
                        ItemIndex = excluded.ItemIndex,
                        RevisionId = excluded.RevisionId,
                        Tracks = excluded.Tracks;
                ";

            var uriParam = insertCommand.Parameters.Add("$uri", SqliteType.Text);
            var nameParam = insertCommand.Parameters.Add("$name", SqliteType.Text);
            var descriptionParam = insertCommand.Parameters.Add("$description", SqliteType.Text);
            var indexParam = insertCommand.Parameters.Add("$itemIndex", SqliteType.Integer);
            var addedAtParam = insertCommand.Parameters.Add("$addedAt", SqliteType.Text);
            var revisionIdParam = insertCommand.Parameters.Add("$revisionId", SqliteType.Text);
            var tracksParam = insertCommand.Parameters.Add("$tracks", SqliteType.Text);

            foreach (var item in playlists)
            {
                uriParam.Value = item.Uri;
                nameParam.Value = item.Name;
                descriptionParam.Value = item.Description ?? (object)DBNull.Value;
                indexParam.Value = item.Index;
                addedAtParam.Value = item.AddedAt.ToString("o"); // ISO 8601 format
                revisionIdParam.Value = item.RevisionId;
                tracksParam.Value = item.Tracks != null && item.Tracks.Count > 0
                    ? JsonSerializer.Serialize(item.Tracks, SpotifyClient.DefaultJsonOptions)
                    : (object)DBNull.Value;

                await insertCommand.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();

            _logger.LogInformation("Saved {Count} cached playlists.", playlists.Count);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to save cached playlists.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SavePlaylist(SpotifyCachedPlaylistItem item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // Insert or update the playlist item
            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = @"
                    INSERT INTO CachedPlaylists (Uri, Name, Description, ItemIndex, AddedAt, RevisionId, Tracks)
                    VALUES ($uri, $name, $description, $itemIndex, $addedAt, $revisionId, $tracks)
                    ON CONFLICT(Uri) DO UPDATE SET
                        Name = excluded.Name,
                        Description = excluded.Description,
                        ItemIndex = excluded.ItemIndex,
                        RevisionId = excluded.RevisionId,
                        Tracks = excluded.Tracks;
                ";

            insertCommand.Parameters.AddWithValue("$uri", item.Uri);
            insertCommand.Parameters.AddWithValue("$name", item.Name);
            insertCommand.Parameters.AddWithValue("$description", item.Description ?? (object)DBNull.Value);
            insertCommand.Parameters.AddWithValue("$itemIndex", item.Index);
            insertCommand.Parameters.AddWithValue("$addedAt", item.AddedAt.ToString("o")); // ISO 8601 format
            insertCommand.Parameters.AddWithValue("$revisionId", item.RevisionId);
            insertCommand.Parameters.AddWithValue("$tracks", item.Tracks != null && item.Tracks.Count > 0
                ? JsonSerializer.Serialize(item.Tracks, SpotifyClient.DefaultJsonOptions)
                : (object)DBNull.Value);

            await insertCommand.ExecuteNonQueryAsync();

            await transaction.CommitAsync();

            _logger.LogInformation("Saved playlist {Uri} with revision {RevisionId}.", item.Uri, item.RevisionId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to save playlist {Uri} with revision {RevisionId}.", item.Uri,
                item.RevisionId);
            throw;
        }
    }

    public async Task<SpotifyCachedPlaylistItem?> GetPlaylist(string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
                SELECT Uri, Name, Description, ItemIndex, AddedAt, RevisionId, Tracks
                FROM CachedPlaylists
                WHERE Uri = $uri;
            ";

        command.Parameters.AddWithValue("$uri", id);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        var uri = reader.GetString(0);
        var name = reader.GetString(1);
        var description = reader.IsDBNull(2) ? null : reader.GetString(2);
        var index = reader.GetInt32(3);
        var addedAt = DateTimeOffset.Parse(reader.GetString(4));
        var revisionId = reader.GetString(5);
        var tracksJson = reader.IsDBNull(6) ? null : reader.GetString(6);

        var tracks = tracksJson != null
            ? JsonSerializer.Deserialize<List<SpotifyCachedPlaylistTrack>>(tracksJson, SpotifyClient.DefaultJsonOptions)
            : new List<SpotifyCachedPlaylistTrack>();

        var item = new SpotifyCachedPlaylistItem
        {
            Uri = uri,
            Name = name,
            Description = description,
            Index = index,
            AddedAt = addedAt,
            RevisionId = revisionId,
            Tracks = tracks
        };

        _logger.LogInformation("Retrieved playlist {Uri} with revision {RevisionId}.", item.Uri, item.RevisionId);
        return item;
    }
}