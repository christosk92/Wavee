using LinqToDB;
using LinqToDB.Data;
using Spotify.Metadata;
using Wavee.Spotify.Cache.Entities;

namespace Wavee.Spotify.Cache;

public class LocalCacheContext : DataConnection
{
    public LocalCacheContext(string sqLite, string connString) : base(sqLite, connString)
    {
        try
        {
            this.CreateTable<TrackEntity>();
        }
        catch (Exception e)
        {
        }

        try
        {
            this.CreateTable<FileEntity>();
        }
        catch (Exception e)
        {
        }
    }

    public ITable<FileEntity> File => GetTable<FileEntity>();
    public ITable<TrackEntity> Track => GetTable<TrackEntity>();
}