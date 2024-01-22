namespace Wavee.Vorbis.Decoder.Setup.Codebooks;

internal interface ICodebookEntry<EValueType>
{
    bool IsJump { get; }
    bool IsValue { get; }
    EValueType Value { get; }
    uint JumpLength { get; }
    uint JumpOffsetMax { get; }
    uint JumpOffset { get; }
    uint ValueLen { get; }
    ICodebookEntry<EValueType> new_jump(uint offset, byte length);
    ICodebookEntry<EValueType> new_value(EValueType value, byte length);
}