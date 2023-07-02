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
}