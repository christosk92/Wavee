using Wavee.UI.Identity.Users.Contracts;
using Wavee.UI.Models;

namespace Wavee.UI.AudioImport;

public record GroupedAlbum : IAlbum
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
}