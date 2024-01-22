namespace Wavee.Vorbis.Decoder.Setup.Codebooks;

internal class CodebookBlock<EValueType> where EValueType : unmanaged
{
    public CodebookBlock(byte Width, SortedDictionary<ushort, int> Nodes, List<CodebookValue<EValueType>> Values)
    {
        this.Width = Width;
        this.Nodes = Nodes;
        this.Values = Values;
    }

    public byte Width { get; set; }
    public SortedDictionary<ushort, int> Nodes { get; init; } 
    public List<CodebookValue<EValueType>> Values { get; init; }

    public void Deconstruct(out byte Width, out SortedDictionary<ushort, int> Nodes, out List<CodebookValue<EValueType>> Values)
    {
        Width = this.Width;
        Nodes = this.Nodes;
        Values = this.Values;
    }
}