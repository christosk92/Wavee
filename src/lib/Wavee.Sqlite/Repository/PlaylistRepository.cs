using Eum.Spotify.context;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Wavee.Sqlite.Entities;
using Z.BulkOperations;

namespace Wavee.Sqlite.Repository;

public class PlaylistRepository
{
    private readonly string _cachePath;

    public PlaylistRepository(string cachePath)
    {
        _cachePath = cachePath;
    }

    public async Task<Unit> InsertPlaylist(CachedPlaylist playlist)
    {
        await using var db = LocalDbFactory.Create(_cachePath);
        await db.Playlists.AddAsync(playlist);

        foreach (var playlistTrack in playlist.PlaylistTracks)
        {
            await db.PlaylistTracks.AddAsync(playlistTrack);
        }

        await db.SaveChangesAsync();

        return Unit.Default;
    }

    public async Task<CachedPlaylist?> GetPlaylist(string id)
    {
        await using var db = LocalDbFactory.Create(_cachePath);
        var playlist = await db.Playlists
            .Include(x => x.PlaylistTracks)
            .FirstOrDefaultAsync(x => x.Id == id);

        return playlist;
    }

    public async Task SetPlaylist(CachedPlaylist playlist)
    {
        await using var db = LocalDbFactory.Create(_cachePath);
        //insert or update playlist
        var exists = await db.Playlists.AnyAsync(x => x.Id == playlist.Id);
        if (exists)
        {
            db.Playlists.Update(playlist);
            //await db.PlaylistTracks.BulkUpdateAsync(playlist.PlaylistTracks);
        }
        else
        {
            await db.Playlists.AddAsync(playlist);
           // await db.PlaylistTracks.BulkUpdateAsync(playlist.PlaylistTracks);
        }
        await db.SaveChangesAsync();
    }

    private static void Options(BulkOperation<CachedPlaylistTrack> obj)
    {
        //update if exists
        obj.ColumnPrimaryKeyExpression = x => x.Uid;
    }
}