using Wavee.Spotify.Id;

namespace Wavee.UI.Identity.Users.Contracts;

public interface IWaveeUser
{
    string Id { get; }
    ServiceType ServiceType { get; }
}