
namespace Wavee.Vorbis.Format.Tags;

public interface ITagValue { }

public readonly record struct BinaryTagValue(byte[] Value) : ITagValue;
public readonly record struct BooleanTagValue(bool Value) : ITagValue;
public readonly record struct FlagTagValue : ITagValue;
public readonly record struct FloatTagValue(double Value) : ITagValue;
public readonly record struct SignedIntegerTagValue(long Value) : ITagValue;
public readonly record struct StringTagValue(string Value) : ITagValue;
public readonly record struct UnsignedIntegerTagValue(ulong Value) : ITagValue;