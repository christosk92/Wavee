using System.Collections.Immutable;
using LanguageExt;

namespace Wavee.Spfy.Items;

public readonly record struct SpotifySimpleAlbum : ISpotifyItem, IWaveeAlbum
{
    public required SpotifyId Uri { get; init; }
    public required string Name { get; init; }
    public required Seq<UrlImage> Images { get; init; }
    public required Seq<IWaveeAlbumArtist> Artists { get; init; }
    public string Id => Uri.ToString();
    public required int Year { get; init; }
    public required string Type { get; init; }

    public int TotalTracks => Tracks.Length;

    public required Seq<(SpotifyId Id, ushort Disc)> Tracks { get; init; }
}
public readonly record struct SpotifyTrackAlbum : ISpotifyPlayableItemGroup, IWaveeTrackAlbum
{
    public required SpotifyId Uri { get; init; }
    public required string Name { get; init; }
    public required Seq<UrlImage> Images { get; init; }
    public required Seq<WaveePlayableItemDescription> Artists { get; init; }

    public string Id => Uri.ToString();
    public required int Year { get; init; }
}