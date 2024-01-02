using Tango.Types;
using Wavee.Spotify.Interfaces.Models;

namespace Wavee.Spotify.Core.Models.Playback;

public readonly record struct SpotifySimpleContext
{
    public required Option<ISpotifyItem> Item { get; init; }
    public required string Uri { get; init; }
    public required IReadOnlyDictionary<string, string> Metadata { get; init; }
}