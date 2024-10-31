namespace Wavee.Core.Decoders.VorbisDecoder.Decoder.Setup.Codebooks;

internal readonly record struct Entry32x32(uint Value, uint Offset) : ICodebookEntry<uint>
{
    public const uint JumpOffestMax = 0x7fff_ffff;
    public const uint JumpFlag = 0x8000_0000;
    public uint JumpLength => Value;
    public uint JumpOffsetMax => JumpOffestMax;
    public uint JumpOffset => (Offset & ~JumpFlag);
    public uint ValueLen => Offset & ~JumpFlag;
    public bool IsJump => (Offset & JumpFlag) != 0;
    public bool IsValue => (Offset & JumpFlag) == 0;

    public ICodebookEntry<uint> new_jump(uint offset, byte length)
    {
        return new Entry32x32(length, offset | JumpFlag);
    }

    public ICodebookEntry<uint> new_value(uint value, byte length)
    {
        return new Entry32x32(value, length);
    }
}