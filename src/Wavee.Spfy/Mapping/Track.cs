using System.Collections.Immutable;
using Spotify.Metadata;
using Wavee.Spfy.Items;

namespace Wavee.Spfy.Mapping;

internal static class TracksMapping
{
    public static SpotifySimpleTrack MapToDto(this Track track)
    {
        if (track.File.Count is 0)
        {
            foreach (var alternative in track.Alternative)
            {
                track.File.AddRange(alternative.File);
            }
        }
        return new SpotifySimpleTrack
        {
            Uri = SpotifyId.FromRaw(track.Gid.Span,
                AudioItemType.Track),
            Name = track.Name,
            Descriptions = track.Artist.Select(f => f.MapToDescription())
                .ToSeq(),
            Group = track.Album.MapToGroup(track),
            DiscNumber = (uint)track.DiscNumber,
            TrackNumber = (uint)track.Number,
            AudioFiles = track.File.Select(f => f.MapToDto())
                .ToSeq(),
            PreviewFiles = track.Preview.Select(f => f.MapToDto())
                .ToSeq(),
            Duration = TimeSpan.FromMilliseconds(track.Duration),
            Explicit = track.Explicit,
        };
    }
}