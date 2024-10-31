using Wavee.Models.Common;

namespace Wavee.Models.Metadata;

public sealed class SpotifyAlbum : SpotifyPlayableItem
{
    public IReadOnlyCollection<SpotifyId> Tracks { get; init; }
}