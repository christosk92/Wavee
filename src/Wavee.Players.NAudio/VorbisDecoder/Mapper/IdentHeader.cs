
namespace Wavee.Vorbis.Mapper;

internal readonly record struct IdentHeader(byte NChannels, uint SampleRate, byte Bs0Exp, byte Bs1Exp);