using Wavee.Spotify.Clients.AudioKeys;
using Wavee.Spotify.Clients.Mercury;
using Wavee.Spotify.Clients.SpApi;

namespace Wavee.Spotify;

public interface ISpotifyClient
{
    IMercuryClient Mercury { get; }
    ISpApi InternalApi { get; }
    IAudioKeys AudioKeys { get;  }
}