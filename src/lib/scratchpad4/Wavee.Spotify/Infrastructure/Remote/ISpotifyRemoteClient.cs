using Wavee.Spotify.Models.Responses;

namespace Wavee.Spotify.Infrastructure.Remote;

public interface ISpotifyRemoteClient
{
    IObservable<SpotifyRemoteState> StateChanged { get; }
}