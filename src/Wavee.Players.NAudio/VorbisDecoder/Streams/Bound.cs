namespace Wavee.Vorbis.Streams;

internal readonly record struct Bound(uint Seq, ulong Ts, ulong Delay);