using Eum.Spotify.playlist4;
using LanguageExt;
using Wavee.Core.Ids;
using Wavee.Spotify.Infrastructure.Playback.Contracts;

namespace Wavee.Spotify.Infrastructure.Remote.Contracts;

public interface ISpotifyRemoteClient
{
    IObservable<Option<SpotifyRemoteState>> StateUpdates { get; }
    IObservable<SpotifyRootlistUpdateNotification> RootlistChanged { get;  }
    IObservable<SpotifyLibraryUpdateNotification> LibraryChanged { get; }
    Task<Option<Unit>> Takeover(CancellationToken ct = default);
    Task<Unit> RefreshState();
    IObservable<Diff> ObservePlaylist(AudioId id);
}