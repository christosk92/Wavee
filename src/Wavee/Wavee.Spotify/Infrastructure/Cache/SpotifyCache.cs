using System;
using System.Data.SQLite;
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using LanguageExt;
using LanguageExt.SomeHelp;
using LanguageExt.UnsafeValueAccess;
using LiteDB;
using Org.BouncyCastle.Crypto;
using Spotify.Metadata;
using Wavee.Core.Ids;
using Wavee.Spotify.Infrastructure.Mercury.Models;
using Wavee.Spotify.Infrastructure.PrivateApi.Contracts.Response;
using static System.Runtime.InteropServices.JavaScript.JSType;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Wavee.Spotify.Infrastructure.Cache;

public readonly struct SpotifyCache : ISpotifyCache
{
    private readonly Option<string> _audioFilesRoot;
    private readonly Option<string> _connString;
    private static bool _initialized;
    public SpotifyCache(Option<string> root, string en)
    {
        _connString = root.Map(r =>
        {
            var path = Path.Combine(r, $"cache_dt_{en}.db");
            return $"Data Source={path};Version=3;";
        });
        _audioFilesRoot = root.Map(r => Path.Combine(r, "audiofiles"));
        if (!Initialized && _connString.IsSome)
        {
            Initialized = true;
            static void InitializeRawEntitiesTable(string connString)
            {
                using (var connection = new SQLiteConnection(connString))
                {
                    connection.Open();

                    string sql = @"CREATE TABLE IF NOT EXISTS raw (
                            id TEXT PRIMARY KEY,
                            data BLOB NOT NULL,
                            expirationText TEXT NOT NULL
                        );";

                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }

            static void InitializeTracksTable(string connString)
            {
                using (var connection = new SQLiteConnection(connString))
                {
                    connection.Open();

                    const string sql = @"CREATE TABLE IF NOT EXISTS tracks (
                                id TEXT PRIMARY KEY,
                                name TEXT NOT NULL,
                                artist_space TEXT NOT NULL,
                                album_name TEXT NOT NULL,
                                duration TEXT NOT NULL,
                                can_play INTEGER NOT NULL,
                                data BLOB NOT NULL
                            );";

                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }

            InitializeTracksTable(_connString.ValueUnsafe());

            InitializeRawEntitiesTable(_connString.ValueUnsafe());
        }
    }

    public static bool Initialized { get; set; }

    public Option<Stream> AudioFile(AudioFile file)
    {
        //TODO:
        //files are stored in a folder structure like this:
        //audiofiles/spotify/{fileId}
        //where {fileId} is the id of the file
        var path = _audioFilesRoot.Map(r => Path.Combine(r, ToBase16(file)));
        if (path.IsSome && File.Exists(path.ValueUnsafe()))
        {
            return File.OpenRead(path.ValueUnsafe());
        }

        return Option<Stream>.None;
    }

    public Unit SaveAudioFile(AudioFile file, byte[] data)
    {
        var path = _audioFilesRoot.Map(r => Path.Combine(r, ToBase16(file)));
        if (path.IsSome)
        {
            //create subfolders if they don't exist
            Directory.CreateDirectory(Path.GetDirectoryName(path.ValueUnsafe())!);
            File.WriteAllBytes(path.ValueUnsafe(), data);
        }

        return Unit.Default;
    }

    public Option<TrackOrEpisode> Get(AudioId audioId)
    {
        if (_connString.IsNone) return Option<TrackOrEpisode>.None;

        using var connection = new SQLiteConnection(_connString.ValueUnsafe());
        connection.Open();

        const string sql = @"SELECT data FROM tracks WHERE id = @id;";

        using var command = new SQLiteCommand(sql, connection);
        command.Parameters.AddWithValue("@id", audioId.ToString());
        command.ExecuteNonQuery();

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            var data = (byte[])reader["data"];
            return audioId.Type switch
            {
                AudioItemType.PodcastEpisode => new TrackOrEpisode(Episode.Parser.ParseFrom(data)),
                AudioItemType.Track => new TrackOrEpisode(
                    Either<Episode, Lazy<Track>>.Right(new Lazy<Track>(() => Track.Parser.ParseFrom(data)))),
            };
        }

        return Option<TrackOrEpisode>.None;
    }

    public Unit Save(TrackOrEpisode fetchedTrack)
    {
        if (_connString.IsNone) return Unit.Default;
        using var connection = new SQLiteConnection(_connString.ValueUnsafe());
        connection.Open();

        const string sql =
            @"INSERT INTO tracks (id, name, artist_space, album_name, duration, can_play, data) VALUES (@id, @name, @artistSpace, @albumName, @duration, @canPlay, @data);";

        using var command = new SQLiteCommand(sql, connection);
        command.Parameters.AddWithValue("@id", fetchedTrack.Id.ToString());
        command.Parameters.AddWithValue("@name", fetchedTrack.Name);
        command.Parameters.AddWithValue("@artistSpace", string.Join(" ", fetchedTrack.Artists.Select(c => c.Name)));
        command.Parameters.AddWithValue("@albumName", fetchedTrack.Group.Name);
        command.Parameters.AddWithValue("@duration", fetchedTrack.Duration.ToString());
        command.Parameters.AddWithValue("@canPlay", fetchedTrack.CanPlay ? 1 : 0);
        command.Parameters.AddWithValue("@data", fetchedTrack.Value.Match(
            Left: e => e.ToByteArray(),
            Right: t => t.Value.ToByteArray()
        ));

        command.ExecuteNonQuery();

        return Unit.Default;
    }

    public bool[] CheckExists(Seq<AudioId> request)
    {
        bool[] exists = new bool[request.Length];
        if (_connString.IsNone) return exists;

        var ids = request.Map(id => id.ToString()).ToArray();

        using var connection = new SQLiteConnection(_connString.ValueUnsafe());
        connection.Open();

        // Create a temporary table
        using (var cmd = new SQLiteCommand("CREATE TEMPORARY TABLE IF NOT EXISTS temp_ids (id TEXT)", connection))
        {
            cmd.ExecuteNonQuery();
        }

        // Begin transaction
        using var transaction = connection.BeginTransaction();

        // Insert ids into temporary table
        using (var cmd = new SQLiteCommand("INSERT INTO temp_ids (id) VALUES (@id)", connection))
        {
            var param = cmd.Parameters.Add("@id", System.Data.DbType.String);

            foreach (var id in ids)
            {
                param.Value = id;
                cmd.ExecuteNonQuery();
            }
        }

        // Query to find matches
        const string sql = "SELECT tracks.id FROM temp_ids JOIN tracks ON temp_ids.id = tracks.id;";

        using (var command = new SQLiteCommand(sql, connection))
        {
            var idSet = new System.Collections.Generic.HashSet<string>();

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    idSet.Add(reader.GetString(0));
                }
            }

            for (int i = 0; i < ids.Length; i++)
            {
                exists[i] = idSet.Contains(ids[i]);
            }
        }

        // Drop the temporary table
        using (var cmd = new SQLiteCommand("DROP TABLE temp_ids", connection))
        {
            cmd.ExecuteNonQuery();
        }

        // Commit transaction
        transaction.Commit();

        return exists;
    }
    public Unit SaveBulk(Seq<TrackOrEpisode> result)
    {
        if (_connString.IsNone) return Unit.Default;

        var tracks = result.Where(c => c.Id.Type is AudioItemType.Track);
        var episodes = result.Where(c => c.Id.Type is AudioItemType.PodcastEpisode);


        //Tracks first
        using (var connection = new SQLiteConnection(_connString.ValueUnsafe()))
        {
            connection.Open();

            using (var transaction = connection.BeginTransaction())
            {
                const string sql =
                    @"INSERT INTO tracks (id, name, artist_space, album_name, duration, can_play, data) VALUES (@id, @name, @artistSpace, @albumName, @duration, @canPlay, @data);";

                using (var command = new SQLiteCommand(sql, connection))
                {
                    // Adding parameters outside the loop
                    command.Parameters.Add("@id", System.Data.DbType.String);
                    command.Parameters.Add("@name", System.Data.DbType.String);
                    command.Parameters.Add("@artistSpace", System.Data.DbType.String);
                    command.Parameters.Add("@albumName", System.Data.DbType.String);
                    command.Parameters.Add("@duration", System.Data.DbType.String);
                    command.Parameters.Add("@canPlay", System.Data.DbType.Int32);
                    command.Parameters.Add("@data", System.Data.DbType.Binary);

                    // Inserting each track
                    foreach (var track in tracks)
                    {
                        var asTrack = track.AsTrack;

                        command.Parameters["@id"].Value = track.Id;
                        command.Parameters["@name"].Value = asTrack.Name;
                        command.Parameters["@artistSpace"].Value = string.Join(" ", asTrack.Artist.Select(f => f.Name));
                        command.Parameters["@albumName"].Value = asTrack.Album.Name;
                        command.Parameters["@duration"].Value = asTrack.Duration.ToString();
                        command.Parameters["@canPlay"].Value = track.CanPlay ? 1 : 0;
                        command.Parameters["@data"].Value = asTrack.ToByteArray();

                        command.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
            }
        }

        //Episodes second
        //TODO:

        return Unit.Default;
    }

    public Option<ReadOnlyMemory<byte>> GetRawEntity(string id)
    {
        if (_connString.IsNone) return Option<ReadOnlyMemory<byte>>.None;

        using (var connection = new SQLiteConnection(_connString.ValueUnsafe()))
        {
            connection.Open();

            const string sql = "SELECT data, expirationText FROM raw WHERE id = @id;";

            using (var command = new SQLiteCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@id", id);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        ReadOnlyMemory<byte> data = (byte[])reader["data"];
                        //  DateTimeOffset expiration = DateTimeOffset.Parse(reader["expiration"].ToString());
                        var exp = reader["expirationText"].ToString();
                        var explong = long.Parse(exp);
                        var expiration = DateTimeOffset.FromUnixTimeMilliseconds(explong);


                        if (expiration > DateTimeOffset.UtcNow)
                        {
                            return data;
                        }
                    }
                }
            }
        }

        return Option<ReadOnlyMemory<byte>>.None;
    }

    public Unit SaveRawEntity(string Id, string title, byte[] data, DateTimeOffset expiration)
    {
        if (_connString.IsNone) return Unit.Default;

        using (var connection = new SQLiteConnection(_connString.ValueUnsafe()))
        {
            connection.Open();

            const string sql = @"INSERT OR REPLACE INTO raw (id, data, expirationText) VALUES (@id, @data, @expirationText);";

            using (var command = new SQLiteCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@id", Id.ToString());
                command.Parameters.AddWithValue("@data", data);
                command.Parameters.AddWithValue("@expirationText", expiration.ToUnixTimeMilliseconds().ToString());

                command.ExecuteNonQuery();
            }
        }

        return Unit.Default;
    }
    public Option<SpotifyColors> GetColorFor(string imageUrl)
    {
        var id = $"color:{imageUrl}";

        if (_connString.IsNone) return Option<SpotifyColors>.None;

        var bytes = GetRawEntity(id);

        if (bytes.IsNone) return Option<SpotifyColors>.None;

        return JsonSerializer.Deserialize<SpotifyColors>(bytes.ValueUnsafe().Span);
    }

    public Unit SaveColorFor(string imageUrl, SpotifyColors response)
    {
        var id = $"color:{imageUrl}";

        if (_connString.IsNone) return Unit.Default;

        var bytes = JsonSerializer.SerializeToUtf8Bytes(response);

        SaveRawEntity(id, string.Empty, bytes, DateTimeOffset.MaxValue);

        return Unit.Default;
    }

    public async Task<List<TrackOrEpisode>> GetTracksOriginalSort(Seq<AudioId> ids, string filterString)    
    {
        var result = new List<TrackOrEpisode>();

        if (_connString.IsNone) return result;

        var indices = ids.Select((id, index) => (id, index)).ToDictionary(c => c.id, c => c.index);

        var placeholders = string.Join(", ", ids.Select((id, index) => $"@id{index}"));

        // Construct the base SQL query
        var sqlBuilder = new StringBuilder($@"SELECT id, data
                                           FROM tracks
                                           WHERE id IN ({placeholders})");

        // Append the filter clause if the filter string is not null or empty
        if (!string.IsNullOrEmpty(filterString))
        {
            sqlBuilder.Append(" AND (name LIKE @filterString OR artist LIKE @filterString)");
        }


        // Append the limit and offset clauses
        //sqlBuilder.Append(" LIMIT @take OFFSET @skip");

        await using var connection = new SQLiteConnection(_connString.ValueUnsafe());
        connection.Open();

        await using var command = new SQLiteCommand(sqlBuilder.ToString(), connection);
        // Bind ids to the placeholders
        for (int i = 0; i < ids.Count; i++)
        {
            command.Parameters.AddWithValue($"@id{i}", ids[i].ToString());
        }

        // Bind filter string if not null or empty
        if (!string.IsNullOrEmpty(filterString))
        {
            command.Parameters.AddWithValue("@filterString", $"%{filterString}%");
        }

        // // Bind additional parameters
        // command.Parameters.AddWithValue("@take", take);
        // command.Parameters.AddWithValue("@skip", skip);

        // Execute the query
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            //string id = reader["id"].ToString();
            var data = (byte[])reader["data"];

            // Assuming TrackOrEpisode is a class that holds track information.
            // Construct it according to your model.
            var track = new TrackOrEpisode(new Lazy<Track>(() => Track.Parser.ParseFrom(data)));

            result.Add(track);
        }

        return result.OrderBy(x=> indices[x.Id]).ToList();
    }

    static string ToBase16(AudioFile file)
    {
        var bytes = file.FileId.Span;
        var hex = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            hex.AppendFormat("{0:x2}", b);
        }

        return hex.ToString();
    }
}