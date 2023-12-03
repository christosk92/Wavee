using Wavee.Spotify.Common;
using Wavee.Spotify.Domain.Common;

namespace Wavee.Spotify.Domain.Artist;

public sealed class SpotifySimpleArtist : ISpotifyItem 
{
    public required SpotifyId Uri { get; init; }
    public required string Name { get; init; }
    public IReadOnlyCollection<SpotifyImage> Images { get; init; }
}