using LanguageExt;
using Wavee.Spotify.Infrastructure.Remote.Contracts;

namespace Wavee.Spotify.Infrastructure.Playback.Contracts;

public interface ISpotifyPlaybackClient
{
    Task<Unit> Play(string contextUri, Option<int> indexInContext, CancellationToken ct = default);
}