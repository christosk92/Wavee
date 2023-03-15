using Wavee.UI.Identity.Users.Contracts;
using Wavee.UI.ViewModels.Album;

namespace Wavee.UI.Models.AudioItems;
public interface IAlbum : IPlayableItem
{
    ServiceType ServiceType
    {
        get;
    }
    string Name
    {
        get;
    }

    string? Image
    {
        get;
    }

    public string[] Artists
    {
        get;
    }

    public int Tracks
    {
        get;
    }

    ushort ReleaseYear
    {
        get;
    }

    IEnumerable<string> GetPlaybackIds();
}
