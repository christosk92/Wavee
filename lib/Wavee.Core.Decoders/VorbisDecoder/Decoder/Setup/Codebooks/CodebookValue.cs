namespace Wavee.Core.Decoders.VorbisDecoder.Decoder.Setup.Codebooks;

internal readonly record struct CodebookValue<EValueType>(ushort Prefix, byte Width, EValueType Value)where EValueType : unmanaged;