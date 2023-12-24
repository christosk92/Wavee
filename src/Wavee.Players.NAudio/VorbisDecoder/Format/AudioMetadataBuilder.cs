using Wavee.Vorbis.Format;

namespace Wavee.Audio.Meta;

public record VorbisMetadataBuilder
{
    public MetadataRevision Metadata { get; init; } = new();
}