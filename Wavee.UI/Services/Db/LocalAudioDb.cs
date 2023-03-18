using System.Text.Json;
using Dapper;
using Microsoft.Data.Sqlite;
using Wavee.Interfaces.Models;
using Wavee.UI.Interfaces.Services;
using Wavee.UI.Models.Local;

namespace Wavee.UI.Services.Db
{
    public class LocalAudioDb : ILocalAudioDb
    {
        private readonly string _connectionString;

        public LocalAudioDb(IAppDataProvider appDataProvider)
        {
            var path = Path.Combine(appDataProvider.GetAppDataRoot(), "localdata.db");
            _connectionString = $"Data Source={path}";
            InitializeDatabase();
        }

        public async Task<IEnumerable<LocalTrack>> GetLatestImportsAsync(int count, CancellationToken ct = default)
        {
            // // //test import
            // var testTrack = new LocalTrack
            // {
            //     Album = "test",
            //     Performers = new[]
            //     {
            //         "test"
            //     },
            //     Title = "test",
            //     Id = Guid.NewGuid().ToString(),
            //     DateImported = DateTime.UtcNow,
            //     LastChanged = DateTime.UtcNow,
            //     AlbumArtists = Array.Empty<string>(),
            //     AlbumArtistsSort = Array.Empty<string>(),
            //     Genres = Array.Empty<string>(),
            //     PerformersRole = Array.Empty<string>(),
            //     PerformersSort = Array.Empty<string>(),
            //     Composers = Array.Empty<string>(),
            //     ComposersSort = Array.Empty<string>(),
            // };
            // await InsertTrackAsync(testTrack, ct);

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct);

            const string query = @"
            SELECT * FROM MediaItems
            ORDER BY DateImported DESC
            LIMIT @count;
        ";

            var items = await connection.QueryAsync(query, new { count });

            return items.Select(item => (LocalTrack)AdaptToLocalTrack(item, false));
        }


        public async Task InsertTrackAsync(LocalTrack localTrack, CancellationToken ct = default)
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct);

            const string insertQuery = @"
            INSERT INTO MediaItems (
                Id, Image, Title, TitleSort, Subtitle, Description,
                Performers, PerformersSort, PerformersRole,
                AlbumArtists, AlbumArtistsSort,
                Composers, ComposersSort,
                Album, AlbumSort, Comment,
                Genres, Year, Track, TrackCount,
                Disc, DiscCount, Lyrics, Grouping,
                BeatsPerMinute, Conductor, Copyright,
                DateTagged, Publisher, ISRC, Duration, DateImported, LastChanged
            )
            VALUES (
                @Id, @Image, @Title, @TitleSort, @Subtitle, @Description,
                @Performers, @PerformersSort, @PerformersRole,
                @AlbumArtists, @AlbumArtistsSort,
                @Composers, @ComposersSort,
                @Album, @AlbumSort, @Comment,
                @Genres, @Year, @Track, @TrackCount,
                @Disc, @DiscCount, @Lyrics, @Grouping,
                @BeatsPerMinute, @Conductor, @Copyright,
                @DateTagged, @Publisher, @ISRC, @Duration, @DateImported, @LastChanged
            );
        ";

            var dateTagged = localTrack.DateTagged?.ToString("o");
            var parameters = new
            {
                Id = localTrack.Id,
                Image = localTrack.Image,
                Title = localTrack.Title,
                TitleSort = localTrack.TitleSort,
                Subtitle = localTrack.Subtitle,
                Description = localTrack.Description,
                Performers = JsonSerializer.Serialize(localTrack.Performers),
                PerformersSort = JsonSerializer.Serialize(localTrack.PerformersSort),
                PerformersRole = JsonSerializer.Serialize(localTrack.PerformersRole),
                AlbumArtists = JsonSerializer.Serialize(localTrack.AlbumArtists),
                AlbumArtistsSort = JsonSerializer.Serialize(localTrack.AlbumArtistsSort),
                Composers = JsonSerializer.Serialize(localTrack.Composers),
                ComposersSort = JsonSerializer.Serialize(localTrack.ComposersSort),
                Album = localTrack.Album,
                AlbumSort = localTrack.AlbumSort,
                Comment = localTrack.Comment,
                Genres = JsonSerializer.Serialize(localTrack.Genres),
                Year = localTrack.Year,
                Track = localTrack.Track,
                TrackCount = localTrack.TrackCount,
                Disc = localTrack.Disc,
                DiscCount = localTrack.DiscCount,
                Lyrics = localTrack.Lyrics,
                Grouping = localTrack.Grouping,
                BeatsPerMinute = localTrack.BeatsPerMinute,
                Conductor = localTrack.Conductor,
                Copyright = localTrack.Copyright,
                DateTagged = dateTagged,
                Publisher = localTrack.Publisher,
                ISRC = localTrack.ISRC,
                Duration = localTrack.Duration,
                DateImported = localTrack.DateImported,
                LastChanged = localTrack.LastChanged,
            };

            await connection.ExecuteAsync(insertQuery, parameters);
        }

        public async Task UpdateTrackAsync(LocalTrack localTrack, CancellationToken ct = default)
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct);

            const string updateQuery = @"
        UPDATE MediaItems SET
            Image = @Image,
            Title = @Title,
            TitleSort = @TitleSort,
            Subtitle = @Subtitle,
            Description = @Description,
            Performers = @Performers,
            PerformersSort = @PerformersSort,
            PerformersRole = @PerformersRole,
            AlbumArtists = @AlbumArtists,
            AlbumArtistsSort = @AlbumArtistsSort,
            Composers = @Composers,
            ComposersSort = @ComposersSort,
            Album = @Album,
            AlbumSort = @AlbumSort,
            Comment = @Comment,
            Genres = @Genres,
            Year = @Year,
            Track = @Track,
            TrackCount = @TrackCount,
            Disc = @Disc,
            DiscCount = @DiscCount,
            Lyrics = @Lyrics,
            Grouping = @Grouping,
            BeatsPerMinute = @BeatsPerMinute,
            Conductor = @Conductor,
            Copyright = @Copyright,
            DateTagged = @DateTagged,
            Publisher = @Publisher,
            ISRC = @ISRC,
            Duration = @Duration,
            DateImported = @DateImported,
            LastChanged = @LastChanged
        WHERE Id = @Id;
    ";

            var dateTagged = localTrack.DateTagged?.ToString("o");
            var parameters = new
            {
                Id = localTrack.Id,
                Image = localTrack.Image,
                Title = localTrack.Title,
                TitleSort = localTrack.TitleSort,
                Subtitle = localTrack.Subtitle,
                Description = localTrack.Description,
                Performers = JsonSerializer.Serialize(localTrack.Performers),
                PerformersSort = JsonSerializer.Serialize(localTrack.PerformersSort),
                PerformersRole = JsonSerializer.Serialize(localTrack.PerformersRole),
                AlbumArtists = JsonSerializer.Serialize(localTrack.AlbumArtists),
                AlbumArtistsSort = JsonSerializer.Serialize(localTrack.AlbumArtistsSort),
                Composers = JsonSerializer.Serialize(localTrack.Composers),
                ComposersSort = JsonSerializer.Serialize(localTrack.ComposersSort),
                Album = localTrack.Album,
                AlbumSort = localTrack.AlbumSort,
                Comment = localTrack.Comment,
                Genres = JsonSerializer.Serialize(localTrack.Genres),
                Year = localTrack.Year,
                Track = localTrack.Track,
                TrackCount = localTrack.TrackCount,
                Disc = localTrack.Disc,
                DiscCount = localTrack.DiscCount,
                Lyrics = localTrack.Lyrics,
                Grouping = localTrack.Grouping,
                BeatsPerMinute = localTrack.BeatsPerMinute,
                Conductor = localTrack.Conductor,
                Copyright = localTrack.Copyright,
                DateTagged = dateTagged,
                Publisher = localTrack.Publisher,
                ISRC = localTrack.ISRC,
                Duration = localTrack.Duration,
                DateImported = localTrack.DateImported,
                LastChanged = localTrack.LastChanged,
            };

            await connection.ExecuteAsync(updateQuery, parameters);
        }

        public bool[] CheckIfAudioFilesExist(IList<string> paths)
        {
            if (paths == null || paths.Count == 0)
            {
                return Array.Empty<bool>();
            }

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            const string query = $@"
            SELECT Id FROM MediaItems
            WHERE Id IN @ids;
        ";

            var existingIds = connection.Query<string>(query, new { ids = paths });

            var existsLookup = existingIds.ToHashSet();

            return paths.Select(id => existsLookup.Contains(id)).ToArray();
        }

        public async Task<IEnumerable<ShortLocalTrack>> GetAllForUpdateCheck()
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            const string query = "SELECT Id, LastChanged  FROM MediaItems";

            var items = await connection.QueryAsync(query);
            return items.Select(a => new ShortLocalTrack
            {
                Id = a.Id,
                LastChanged = DateTime.Parse(a.LastChanged)
            });
        }

        public async Task Remove(string id)
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            const string query = "DELETE FROM MediaItems WHERE Id = @id;";

            await connection.ExecuteAsync(query, new { id });
        }

        public LocalTrack? ReadTrack(string sql)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            //The query =  "SELECT Id, LastChanged  FROM MediaItems " and then the query is added to it;
            var query = "SELECT * FROM MediaItems " + sql;
            var item = connection.QueryFirstOrDefault(query);
            if (item is null) return null;
            return AdaptToLocalTrack(item, false);
        }

        public async Task<IEnumerable<LocalTrack>> ReadTracks(string sql,
            bool withJoinOnPlaycount,
            CancellationToken ct = default)
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct);
            var items = await connection.QueryAsync(sql);
            if (items is null) return Enumerable.Empty<LocalTrack>();
            return items.Select(a => (LocalTrack)AdaptToLocalTrack(a, withJoinOnPlaycount));
        }


        public int Count(string sql)
        {
            //count the number of tracks in the database based on the query
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var count = connection.ExecuteScalar<int>(sql);
            return count;
        }
        public Task<int> Count()
        {
            //count the number of tracks in the database based on the query
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            const string query = "SELECT COUNT(*) FROM MediaItems";
            return connection.ExecuteScalarAsync<int>(query);
        }



        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            const string createTableQuery = @"
            CREATE TABLE IF NOT EXISTS MediaItems (
                Id TEXT PRIMARY KEY,
                Image TEXT,
                Title TEXT,
                TitleSort TEXT,
                Subtitle TEXT,
                Description TEXT,
                Performers JSON,
                PerformersSort JSON,
                PerformersRole JSON,
                AlbumArtists JSON,
                AlbumArtistsSort JSON,
                Composers JSON,
                ComposersSort JSON,
                Album TEXT,
                AlbumSort TEXT,
                Comment TEXT,
                Genres JSON,
                Year INTEGER,
                Track INTEGER,
                TrackCount INTEGER,
                Disc INTEGER,
                DiscCount INTEGER,
                Lyrics TEXT,
                Grouping TEXT,
                BeatsPerMinute INTEGER,
                Conductor TEXT,
                Copyright TEXT,
                DateTagged TEXT,
                Publisher TEXT,
                ISRC TEXT,
                Duration REAL,
                DateImported TEXT,
                LastChanged TEXT
  );
";
            connection.Execute(createTableQuery);
        }

        private static LocalTrack AdaptToLocalTrack(dynamic item, bool withJoinOnPlaycount)
        {
            return new LocalTrack
            {
                // Deserialize string arrays using JsonConvert
                Performers = item.Performers is not null
                    ? JsonSerializer.Deserialize<string[]>(json: item.Performers)
                    : Array.Empty<string>(),
                PerformersSort = item.PerformersSort is not null
                    ? JsonSerializer.Deserialize<string[]>(json: item.PerformersSort)
                    : Array.Empty<string>(),
                PerformersRole = item.PerformersRole is not null
                    ? JsonSerializer.Deserialize<string[]>(json: item.PerformersRole)
                    : Array.Empty<string>(),
                AlbumArtists = item.AlbumArtists is not null
                    ? JsonSerializer.Deserialize<string[]>(json: item.AlbumArtists)
                    : Array.Empty<string>(),
                AlbumArtistsSort = item.AlbumArtistsSort is not null
                    ? JsonSerializer.Deserialize<string[]>(json: item.AlbumArtistsSort)
                    : Array.Empty<string>(),
                Composers = item.Composers is not null
                    ? JsonSerializer.Deserialize<string[]>(json: item.Composers)
                    : Array.Empty<string>(),
                ComposersSort = item.ComposersSort is not null
                    ? JsonSerializer.Deserialize<string[]>(json: item.ComposersSort)
                    : Array.Empty<string>(),
                Genres = item.Genres is not null
                    ? JsonSerializer.Deserialize<string[]>(json: item.Genres)
                    : Array.Empty<string>(),

                // Copy the rest of the properties
                Id = item.Id,
                Image = item.Image,
                Title = item.Title,
                TitleSort = item.TitleSort,
                Subtitle = item.Subtitle,
                Description = item.Description,
                Album = item.Album,
                AlbumSort = item.AlbumSort,
                Comment = item.Comment,
                Year = (uint)item.Year,
                Track = (uint)item.Track,
                TrackCount = (uint)item.TrackCount,
                Disc = (uint)item.Disc,
                DiscCount = (uint)item.DiscCount,
                Lyrics = item.Lyrics,
                Grouping = item.Grouping,
                BeatsPerMinute = (uint)item.BeatsPerMinute,
                Conductor = item.Conductor,
                Copyright = item.Copyright,
                DateTagged = item.DateTagged,
                Publisher = item.Publisher,
                ISRC = item.ISRC,
                Duration = (double)item.Duration,
                DateImported = DateTime.Parse(item.DateImported),
                LastChanged = DateTime.Parse(item.LastChanged),
                Playcount = withJoinOnPlaycount ? item.Playcount : 0,
                LastPlayed = withJoinOnPlaycount ? DateTime.Parse(item.LastPlayed) : DateTime.MinValue
            };
        }
    }
}