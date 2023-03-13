using Wavee.UI.Identity.Users.Contracts;

namespace Wavee.UI.Models;
public interface IAlbum
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
}
