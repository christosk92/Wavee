using Eum.Spotify.extendedmetadata;
using Google.Protobuf;
using Mediator;
using Wavee.Spotify.Common;

namespace Wavee.Spotify.Application.Metadata.Query;

public sealed class FetchBatchedMetadataQuery : IQuery<IReadOnlyDictionary<string, ByteString?>>
{
    public required string Country { get; init; }
    public required bool AllowCache { get; init; }
    public required IReadOnlyCollection<string> Uris { get; init; }
    public required SpotifyItemType ItemsType { get; init; }
}