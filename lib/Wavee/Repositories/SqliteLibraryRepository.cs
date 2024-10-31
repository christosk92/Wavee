// SqliteLibraryRepository.cs

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Wavee.Enums;
using Wavee.Interfaces;
using Wavee.Models.Common;
using Wavee.Models.Library;

namespace Wavee.Repositories;

/// <summary>
/// Implements the <see cref="ILibraryRepository"/> interface using SQLite for persistent storage.
/// </summary>
public class SqliteLibraryRepository : ILibraryRepository
{
    private readonly string _libraryConnectionString;
    private readonly ILogger<SqliteLibraryRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteLibraryRepository"/> class.
    /// </summary>
    /// <param name="libraryDbPath">The path to the SQLite database file.</param>
    /// <param name="logger">The logger instance.</param>
    public SqliteLibraryRepository(string libraryDbPath, ILogger<SqliteLibraryRepository> logger)
    {
        if (string.IsNullOrWhiteSpace(libraryDbPath))
            throw new ArgumentException("Library database path must be a valid file path.", nameof(libraryDbPath));

        var finalPath = Path.Combine(libraryDbPath, "library.db");
        _libraryConnectionString = $"Data Source={finalPath}";
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        InitializeDatabase();
    }

    /// <summary>
    /// Initializes the library database by creating the necessary tables if they do not exist.
    /// </summary>
    private void InitializeDatabase()
    {
        try
        {
            using var connection = new SqliteConnection(_libraryConnectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS LibraryItems (
                        Id TEXT NOT NULL PRIMARY KEY,
                        Type INTEGER NOT NULL,
                        AddedAt TEXT NOT NULL,
                        Deleted INTEGER NOT NULL,
                        FetchException TEXT
                    );
                ";
            command.ExecuteNonQuery();

            _logger.LogInformation("Library database initialized successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize the library database.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task AddOrUpdateItemsAsync(IEnumerable<SpotifyLibraryItem> items, CancellationToken cancellationToken = default)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));

        using var connection = new SqliteConnection(_libraryConnectionString);
        await connection.OpenAsync(cancellationToken);

        using var transaction = connection.BeginTransaction();

        var command = connection.CreateCommand();
        command.CommandText = @"
                INSERT INTO LibraryItems (Id, Type, AddedAt, Deleted, FetchException)
                VALUES ($id, $type, $addedAt, $deleted, $fetchException)
                ON CONFLICT(Id) DO UPDATE SET
                    Type = excluded.Type,
                    AddedAt = excluded.AddedAt,
                    Deleted = excluded.Deleted,
                    FetchException = excluded.FetchException;
            ";

        var idParam = command.CreateParameter();
        idParam.ParameterName = "$id";
        command.Parameters.Add(idParam);

        var typeParam = command.CreateParameter();
        typeParam.ParameterName = "$type";
        command.Parameters.Add(typeParam);

        var addedAtParam = command.CreateParameter();
        addedAtParam.ParameterName = "$addedAt";
        command.Parameters.Add(addedAtParam);

        var deletedParam = command.CreateParameter();
        deletedParam.ParameterName = "$deleted";
        command.Parameters.Add(deletedParam);

        var fetchExceptionParam = command.CreateParameter();
        fetchExceptionParam.ParameterName = "$fetchException";
        command.Parameters.Add(fetchExceptionParam);

        foreach (var item in items)
        {
            idParam.Value = item.Id.ToString();
            typeParam.Value = (int)item.Id.ItemType;
            addedAtParam.Value = item.AddedAt.ToString("o"); // ISO 8601 format
            deletedParam.Value = item.Deleted ? 1 : 0;
            fetchExceptionParam.Value = item.FetchException?.Message ?? (object)DBNull.Value;

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation("Added/Updated {Count} items in the library repository.", items.Count());
    }

    /// <inheritdoc />
    public async Task DeleteItemsAsync(IEnumerable<SpotifyLibraryItem> items, CancellationToken cancellationToken = default)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));

        using var connection = new SqliteConnection(_libraryConnectionString);
        await connection.OpenAsync(cancellationToken);

        using var transaction = connection.BeginTransaction();

        var command = connection.CreateCommand();
        command.CommandText = @"
                DELETE FROM LibraryItems WHERE Id = $id;
            ";

        var idParam = command.CreateParameter();
        idParam.ParameterName = "$id";
        command.Parameters.Add(idParam);

        foreach (var item in items)
        {
            idParam.Value = item.Id.ToString();
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation("Deleted {Count} items from the library repository.", items.Count());
    }

    /// <inheritdoc />
    public async Task<List<SpotifyLibraryItem>> GetAllItemsAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<SpotifyLibraryItem>();

        using var connection = new SqliteConnection(_libraryConnectionString);
        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Type, AddedAt, Deleted, FetchException FROM LibraryItems;";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = SpotifyId.FromUri(reader.GetString(0));
            var type = (LibraryType)reader.GetInt32(1);
            var addedAt = DateTime.Parse(reader.GetString(2)).ToUniversalTime();
            var deleted = reader.GetInt32(3) == 1;
            
            var libraryItem = new SpotifyLibraryItem(id, addedAt)
            {
                Deleted = deleted
            };
            items.Add(libraryItem);
        }

        _logger.LogInformation("Retrieved {Count} items from the library repository.", items.Count);
        return items;
    }
}
