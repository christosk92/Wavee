using Wavee.Models.Common;

namespace Wavee.Models.Metadata;

public sealed class SpotifyTrackArtist : SpotifyItem
{
    public required SpotifyTrackArtistRole Role { get; set; }
}