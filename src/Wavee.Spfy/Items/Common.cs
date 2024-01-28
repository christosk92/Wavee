using System.Collections.Immutable;
using LanguageExt;
using Spotify.Metadata;
using Wavee.Spfy.Utils;

namespace Wavee.Spfy.Items;

public interface ISpotifyItem : IWaveeItem
{
    SpotifyId Uri { get; }
    Seq<UrlImage> Images { get; }
}

public interface ISpotifyPlayableItem : ISpotifyItem, IWaveePlayableItem
{
    ISpotifyPlayableItemGroup Group { get; }

    Seq<SpotifyAudioFile> AudioFiles { get; }
    Seq<SpotifyAudioFile> PreviewFiles { get; }

    TimeSpan Duration { get; }
    bool Explicit { get; }
}


public readonly struct SpotifyEmptyItem : ISpotifyItem
{
    public SpotifyEmptyItem()
    {
        Name = null;
        Uri = default;
    }

    public required string Name { get; init; }
    public string Id => Id.ToString();
    public required SpotifyId Uri { get; init; }

    public Seq<UrlImage> Images { get; } = Seq<UrlImage>.Empty;
}

public interface ISpotifyPlayableItemGroup : ISpotifyItem
{
    Seq<UrlImage> Images { get; init; }
    int Year { get; }
}

public readonly struct SpotifyAudioFile
{
    public required ReadOnlyMemory<byte> FileId { get; init; }
    public required AudioFile.Types.Format Format { get; init; }
    public string FileIdBase16 => FileId.Span.ToBase16();
}




public sealed class SpotifyFullAlbum : ISpotifyItem, IWaveeAlbum
{
    public required string Name { get; init; }
    public required Seq<IWaveeAlbumArtist> Artists { get; init; }
    public required int Year { get; init; }
    public required string Type { get; init; }
    public required int TotalTracks { get; init; }
    public required SpotifyId Uri { get; init; }
    public required Seq<UrlImage> Images { get; init; }
    public required Seq<IWaveeTrackAlbum> Tracks { get; init; }
    public string Id => Uri.ToString();
}