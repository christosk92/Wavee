using Microsoft.EntityFrameworkCore;
using Wavee.Sqlite.Entities;

namespace Wavee.Sqlite;

public class LocalDbContext : DbContext
{
    public static string DbPath { get; set; }

    public LocalDbContext()
    {
      // DbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Wavee", "wavee.db");
    }
    // The following configures EF to create a Sqlite database file in the
    // special "local" folder for your platform.
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");

    public DbSet<RawEntity> RawEntities { get; set; }
    public DbSet<CachedTrack> Tracks { get; set; }
    public DbSet<CachedPlaylist> Playlists { get; set; }
    public DbSet<CachedPlaylistTrack> PlaylistTracks { get; set; }

}