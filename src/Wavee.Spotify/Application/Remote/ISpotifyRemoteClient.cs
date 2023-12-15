using Eum.Spotify.context;
using Eum.Spotify.playback;

namespace Wavee.Spotify.Application.Remote;

public interface ISpotifyRemoteClient
{
    Task Play(Context context, PlayOrigin playOrigin, PreparePlayOptions preparePlayOptions, string? overrideDeviceId = null);
}