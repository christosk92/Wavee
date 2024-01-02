using Spotify.Metadata;
using Wavee.Spotify.Core.Extension;

namespace Wavee.Spotify.Core.Models.Track;

public readonly struct SpotifyAudioFile
{
    public required ReadOnlyMemory<byte> FileId { get; init; }
    public required AudioFile.Types.Format Format { get; init; }
    public string FileIdBase16 => FileId.Span.ToBase16();
}