using LanguageExt;
using Wavee.Spotify.Infrastructure.Playback.Contracts;

namespace Wavee.Spotify.Infrastructure.Remote.Contracts;

public interface ISpotifyRemoteClient
{
    IObservable<Option<SpotifyRemoteState>> StateUpdates { get; }
}