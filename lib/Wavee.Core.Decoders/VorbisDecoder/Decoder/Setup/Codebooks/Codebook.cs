namespace Wavee.Core.Decoders.VorbisDecoder.Decoder.Setup.Codebooks;

internal readonly record struct Codebook<E, EValueType>(E[] Table, uint MaxCodeLen, uint InitBlockLen) where E : ICodebookEntry<EValueType>;