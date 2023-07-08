using Google.Protobuf.WellKnownTypes;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Spotify.Metadata;
using Wavee.Sqlite.Entities;
using Wavee.Sqlite.Mapping;

namespace Wavee.Sqlite.Repository;

public sealed class TracksRepository
{
    private readonly string _cachePath;

    public TracksRepository(string cachePath)
    {
        _cachePath = cachePath;
    }

    public async Task<Track?> GetTrack(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_cachePath))
        {
            return null;
        }

        await using var db = LocalDbFactory.Create(_cachePath);
        var track = await db.Tracks.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (track is null)
        {
            return null;
        }

        if (track.CacheExpiration < DateTimeOffset.UtcNow)
        {
            db.Tracks.Remove(track);
            await db.SaveChangesAsync(ct);
            return null;
        }
        return track?.ToTrack();
    }

    public async Task<Unit> InsertTrack(string id, Track track, DateTimeOffset expiration, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_cachePath))
        {
            return Unit.Default;
        }

        await using var db = LocalDbFactory.Create(_cachePath);
        var cached = track.ToCachedTrack(id, expiration);
        await db.Tracks.AddAsync(cached, ct);
        await db.SaveChangesAsync(ct);
        return Unit.Default;
    }

    public async Task<Dictionary<string, Option<CachedTrack>>> GetTracks(string[] ids)
    {
        if (string.IsNullOrWhiteSpace(_cachePath))
        {
            var emptyOutput = new Dictionary<string, Option<CachedTrack>>(ids.Length);
            foreach (var id in ids)
            {
                emptyOutput.Add(id, Option<CachedTrack>.None);
            }
            return emptyOutput;
        }

        var output = new Dictionary<string, Option<CachedTrack>>(ids.Length);
        await using var db = LocalDbFactory.Create(_cachePath);
        var tracks = await db.Tracks.Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(
                keySelector: x => x.Id,
                elementSelector: x => x
            );
        foreach (var id in ids)
        {
            if (tracks.TryGetValue(id, out var track))
            {
                output[id] = track;
            }
            else
            {
                output[id] = Option<CachedTrack>.None;
            }
        }

        return output;
    }

    public async Task<Unit> InsertTracks(Dictionary<string, TrackWithExpiration> newTracks)
    {
        if (string.IsNullOrWhiteSpace(_cachePath))
        {
            return Unit.Default;
        }

        await using var db = LocalDbFactory.Create(_cachePath);
        var cachedTracks = newTracks
            .Select(x => x.Value.Track.ToCachedTrack(x.Key, x.Value.Expiration))
            .DistinctBy(x => x.Id);
        await db.Tracks.AddRangeAsync(cachedTracks);
        await db.SaveChangesAsync();
        return Unit.Default;
    }
}

public readonly record struct TrackWithExpiration(Track Track, DateTimeOffset Expiration);
public readonly record struct EpisodeWithExpiration(Episode Track, DateTimeOffset Expiration);