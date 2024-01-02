using System.Collections.Immutable;
using Spotify.Metadata;
using Wavee.Core.Enums;
using Wavee.Spotify.Core.Models.Common;
using Wavee.Spotify.Core.Models.Metadata;

namespace Wavee.Spotify.Core.Mappings;

internal static class TracksMapping
{
    public static SpotifySimpleTrack MapToDto(this Track track)
    {
        return new SpotifySimpleTrack
        {
            Uri = SpotifyId.FromRaw(track.Gid.Span,
                AudioItemType.Track),
            Title = track.Name,
            Descriptions = track.Artist.Select(f => f.MapToDescription())
                .ToImmutableArray(),
            Group = track.Album.MapToGroup(),
            DiscNumber = (uint)track.DiscNumber,
            TrackNumber = (uint)track.Number,
            AudioFiles = track.File.Select(f => f.MapToDto())
                .ToImmutableArray(),
            PreviewFiles = track.Preview.Select(f => f.MapToDto())
                .ToImmutableArray(),
            Duration = TimeSpan.FromMilliseconds(track.Duration),
            Explicit = track.Explicit,
        };
    }
}