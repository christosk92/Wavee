using LanguageExt;
using System;
using Wavee.Id;

namespace Wavee.Remote;

/// <summary>
/// The interface for a Spotify remote client. 
/// </summary>
public interface ISpotifyRemoteClient : IDisposable
{
    IObservable<SpotifyRemoteState> CreateListener();
    IObservable<SpotifyLibraryNotification> CreateLibraryListener();
    Option<SpotifyRemoteState> LatestState { get; }
    IObservable<Unit> CreatePlaylistListener();
}
public readonly record struct SpotifyLibraryNotification(Seq<SpotifyLibraryItem> Id, bool Added);
public readonly record struct SpotifyLibraryItem(SpotifyId Id, Option<DateTimeOffset> AddedAt);
