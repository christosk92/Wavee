namespace Wavee.Vorbis.Decoder.Setup.Codebooks;

internal readonly record struct CodebookValue<EValueType>(ushort Prefix, byte Width, EValueType Value)where EValueType : unmanaged;