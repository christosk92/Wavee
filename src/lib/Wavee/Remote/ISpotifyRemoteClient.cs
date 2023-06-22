using LanguageExt;

namespace Wavee.Remote;

/// <summary>
/// The interface for a Spotify remote client. 
/// </summary>
public interface ISpotifyRemoteClient
{
    IObservable<SpotifyRemoteState> CreateListener();
    Option<SpotifyRemoteState> LatestState { get; }
}