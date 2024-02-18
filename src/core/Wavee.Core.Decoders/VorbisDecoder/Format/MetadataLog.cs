using Wavee.Core.Decoders.VorbisDecoder.Format.Tags;

namespace Wavee.Core.Decoders.VorbisDecoder.Format;

public class MetadataLog
{
    // Represents the time-ordered Metadata revisions.
    private LinkedList<MetadataRevision>
        Revisions { get; set; } = new(); // Assuming MetadataRevision is a class or struct you have defined elsewhere

    // Returns a reference to the metadata inside the log.
    public Metadata Metadata()
    {
        return new Metadata(Revisions);
    }

    // Pushes a new Metadata revision onto the log.
    public void Push(MetadataRevision rev)
    {
        Revisions.AddLast(rev);
    }
}

public class Metadata
{
    private LinkedList<MetadataRevision> _revisions;

    public Metadata(LinkedList<MetadataRevision> revisions)
    {
        _revisions = revisions ?? throw new ArgumentNullException(nameof(revisions));
    }
}

public class MetadataRevision : ICloneable
{
    public List<Tag> Tags { get; set; } = new();
    public List<Visual> Visuals { get; set; } = new();
    public List<VendorData> VendorData { get; set; } = new();

    public object Clone()
    {
        return this.MemberwiseClone();
    }
}