using Wavee.Spotify.Common;

namespace Wavee.Spotify.Domain.Common;

public sealed class SpotifyRecentlyPlayedItem
{
    public required string Uri { get; set; }
    public required DateTimeOffset PlayedAt { get; set; }
    public required string PlayedSubItem { get; init; }
}