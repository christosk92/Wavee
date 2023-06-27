using LanguageExt;
using Wavee.Id;
using Wavee.Metadata.Album;
using Wavee.Metadata.Artist;
using Wavee.Metadata.Common;
using Wavee.UI.Common;

namespace Wavee.UI.Client.Album;

public sealed class WaveeUIAlbumView
{
    public required ServiceType Source { get; init; }
    public required string Id { get; init; } = string.Empty;
    public required string Name { get; init; } = string.Empty;
    public required AlbumArtist[] Artists { get; init; } = Array.Empty<AlbumArtist>();
    public required DateTime ReleaseDate { get; init; }
    public required ReleaseDatePrecisionType ReleaseDatePrecision { get; init; }
    public required string LargeImage { get; init; }
    public required Option<string> DarkColor { get; init; } = string.Empty;
    public required Option<string> LightColor { get; init; } = string.Empty;
    public required CardViewModel[] MoreAlbums { get; init; }
    public required string[] Copyrights { get; init; }

    public required WaveeUIAlbumDisc[] Discs { get; init; } = Array.Empty<WaveeUIAlbumDisc>();
}

public sealed class WaveeUIAlbumDisc
{
    public required IReadOnlyCollection<WaveeUIAlbumTrack> Tracks { get; init; } = Array.Empty<WaveeUIAlbumTrack>();
    public required int DiscNumber { get; init; }
}

public sealed class WaveeUIAlbumTrack
{
    public required Option<string> Uid { get; init; }
    public required ServiceType Source { get; init; }
    public required string Id { get; init; } = string.Empty;
    public required string Name { get; init; } = string.Empty;
    public required ContentRatingType ContentRating { get; init; }
    public required TrackArtist[] Artists { get; init; } = Array.Empty<TrackArtist>();
    public required TimeSpan Duration { get; init; }
    public required Option<ulong> Playcount { get; init; }
    public required bool InLibrary { get; init; }
    public required ushort TrackNumber { get; init; }

    public string FormatNumber(ushort x)
    {
        //1 -> 01.
        //10 -> 10.
        //100 -> 100.

        return $"{x:D2}.";
    }

    public string FormatPlaycount(Option<ulong> ulongs)
    {
        if (ulongs.IsNone) return "< 1,000";
        var x = ulongs.IfNone(0);
        if (x < 1000) return x.ToString();

        //1 million -> 1,000,000
        //1 billion -> 1,000,000,000
        return x.ToString("N0");
    }

    public string FormatDuration(TimeSpan timeSpan)
    {
        return timeSpan.ToString(@"mm\:ss");
    }
}