using Eum.Spotify;
using Wavee.Domain.Playback.Player;
using Wavee.Spotify.Application.Album;
using Wavee.Spotify.Application.Artist;
using Wavee.Spotify.Application.AudioKeys;
using Wavee.Spotify.Application.Library;
using Wavee.Spotify.Application.Search;
using Wavee.Spotify.Application.StorageResolve;
using Wavee.Spotify.Domain.State;
using Wavee.Spotify.Domain.Tracks;
using Wavee.Spotify.Domain.User;

namespace Wavee.Spotify.Common.Contracts;

public interface ISpotifyClient
{
    SpotifyClientConfig Config { get; }
    IWaveePlayer Player { get; }
    ISpotifyTrackClient Tracks { get; }
    ISpotifyAudioKeyClient AudioKeys { get; }
    ISpotifyStorageResolver StorageResolver { get; }
    ISpotifyLibraryClient Library { get; }
    ISpotifyArtistClient Artist { get; }
    ISpotifyAlbumClient Album { get; set; }
    ISpotifySearchClient Search { get;}

    Task<APWelcome> User { get; }
    Task<Me> Initialize(CancellationToken cancellationToken = default);
    event EventHandler<SpotifyPlaybackState> PlaybackStateChanged;
}