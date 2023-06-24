using Wavee.UI.Contracts;
using Wavee.UI.Spotify.Authentication;
using Wavee.UI.User;

namespace Wavee.UI.Spotify;

public sealed class SpotifyEnvironment : IMusicEnvironment
{
    public SpotifyEnvironment(IUserManager userManager)
    {
        AuthService = new SpotifyAuthentication(
            userManager: userManager
        );
    }

    public IMusicServiceAuthentication AuthService { get; }
}
