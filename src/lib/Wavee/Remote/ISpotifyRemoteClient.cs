using LanguageExt;

namespace Wavee.Remote;

/// <summary>
/// The interface for a Spotify remote client. 
/// </summary>
public interface ISpotifyRemoteClient : IDisposable
{
    IObservable<SpotifyRemoteState> CreateListener();
    Option<SpotifyRemoteState> LatestState { get; }
}