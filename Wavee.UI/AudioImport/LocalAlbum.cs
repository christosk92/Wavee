using CommunityToolkit.Mvvm.DependencyInjection;
using Wavee.UI.AudioImport.Database;
using Wavee.UI.Identity.Users.Contracts;
using Wavee.UI.Models.AudioItems;

namespace Wavee.UI.AudioImport;

public record LocalAlbum : IAlbum
{
    public string Album
    {
        get;
        init;
    }

    public ServiceType ServiceType
    {
        get;
        init;
    }

    public string Name => Album;

    public string? Image
    {
        get;
        init;
    }

    public string[] Artists
    {
        get;
        init;
    }

    public int Tracks
    {
        get;
        init;
    }

    public ushort ReleaseYear
    {
        get;
        init;
    }

    public IEnumerable<string> GetPlaybackIds()
    {
        var db = Ioc.Default.GetRequiredService<IAudioDb>();
        return db
            .AudioFiles
            .Find(a => a.Album == Album)
            .OrderBy(a => a.Track)
            .Select(j => j.Path);
    }
}