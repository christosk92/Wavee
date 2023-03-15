using Wavee.UI.Identity.Users.Contracts;

namespace Wavee.UI.Models.AudioItems;
public interface IArtist
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
}
