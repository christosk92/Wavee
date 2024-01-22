using System.Collections.Immutable;
using LanguageExt;
using Spotify.Metadata;
using Wavee.Spfy.Utils;

namespace Wavee.Spfy.Items;

public interface ISpotifyItem : IWaveeItem
{
    SpotifyId Uri { get; }
}

public interface ISpotifyPlayableItem : ISpotifyItem, IWaveePlayableItem
{
    Seq<SpotifyPlayableItemDescription> Descriptions { get; }
    ISpotifyPlayableItemGroup Group { get; }

    Seq<SpotifyAudioFile> AudioFiles { get; }
    Seq<SpotifyAudioFile> PreviewFiles { get; }

    TimeSpan Duration { get; }
    bool Explicit { get; }
}

public readonly record struct SpotifyPlayableItemDescription : ISpotifyItem, IWaveeAlbumArtist
{
    public required string Name { get; init; }

    public string Id => Uri.ToString();

    public required SpotifyId Uri { get; init; }
}

public readonly struct SpotifyEmptyItem : ISpotifyItem
{
    public required string Name { get; init; }
    public string Id => Id.ToString();
    public required SpotifyId Uri { get; init; }
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