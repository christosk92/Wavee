using Wavee.Spotify.Core.Models.Common;
using Wavee.Spotify.Infrastructure.Playback;

namespace Wavee.Spotify.Interfaces.Clients.Playback;

public interface ISpotifyPlaybackClient
{
    ValueTask<SpotifyAudioStream> CreateStream(SpotifyId id, CancellationToken cancellationToken = default);
}