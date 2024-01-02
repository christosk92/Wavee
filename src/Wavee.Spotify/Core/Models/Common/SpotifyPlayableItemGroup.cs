using System.Collections.Immutable;
using Wavee.Core.Models.Common;
using Wavee.Spotify.Core.Models.Common;
using Wavee.Spotify.Interfaces.Models;

namespace Wavee.Spotify.Core.Models.Track;

public readonly record struct SpotifyPlayableItemGroup : ISpotifyItem
{
    public required SpotifyId Uri { get; init; }
    public required string Name { get; init; }
    public required ImmutableArray<UrlImage> Images { get; init; }
}