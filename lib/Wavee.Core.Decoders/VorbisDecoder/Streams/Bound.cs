namespace Wavee.Core.Decoders.VorbisDecoder.Streams;

internal readonly record struct Bound(uint Seq, ulong Ts, ulong Delay);