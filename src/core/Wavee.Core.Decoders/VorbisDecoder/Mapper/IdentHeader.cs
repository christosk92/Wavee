
namespace Wavee.Core.Decoders.VorbisDecoder.Mapper;

internal readonly record struct IdentHeader(byte NChannels, uint SampleRate, byte Bs0Exp, byte Bs1Exp);