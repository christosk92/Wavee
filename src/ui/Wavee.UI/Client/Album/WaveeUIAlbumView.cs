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
    public required SpotifyAlbumArtist[] Artists { get; init; } = Array.Empty<SpotifyAlbumArtist>();
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
    public required IReadOnlyCollection<IWaveeUIAlbumTrack> Tracks { get; init; } = Array.Empty<IWaveeUIAlbumTrack>();
    public required int DiscNumber { get; init; }
}

public sealed class WaveeUIAlbumTrack : IWaveeUIAlbumTrack
{
    public required Option<string> Uid { get; init; }
    public required ServiceType Source { get; init; }
    public required string Id { get; init; } = string.Empty;
    public required string Name { get; init; } = string.Empty;
    public required ContentRatingType ContentRating { get; init; }
    public required ITrackArtist[] Artists { get; init; } = Array.Empty<ITrackArtist>();
    public required TimeSpan Duration { get; init; }
    public required Option<ulong> Playcount { get; init; }
    public required bool InLibrary { get; init; }
    public required ushort TrackNumber { get; init; }
}

public interface IWaveeUIAlbumTrack
{
    Option<string> Uid { get; }
    ServiceType Source { get; }
    string Id { get; }
    string Name { get; }
    ContentRatingType ContentRating { get; }
    ITrackArtist[] Artists { get; }
    TimeSpan Duration { get; }
    Option<ulong> Playcount { get; }
    bool InLibrary { get; }
    ushort TrackNumber { get; }
}