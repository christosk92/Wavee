using Wavee.Spotify.Core.Models.Common;

namespace Wavee.Spotify.Interfaces.Clients.Playback;

internal interface ISpotifyAudioKeyService
{
    Task<SpotifyAudioKey> GetAudioKey(SpotifyId trackId, string fileFileIdBase16, CancellationToken cancellationToken);
}

internal readonly record struct SpotifyAudioKey(byte[]? Key, bool HasKey);  