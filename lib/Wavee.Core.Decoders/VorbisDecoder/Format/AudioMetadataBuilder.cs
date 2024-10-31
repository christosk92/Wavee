namespace Wavee.Core.Decoders.VorbisDecoder.Format;

public record VorbisMetadataBuilder
{
    public MetadataRevision Metadata { get; init; } = new();
}