using Wavee.Playback.Player;

namespace Wavee.Models.Common;

internal class SortDescriptor
{
    public string FieldName { get; set; }
    public bool Descending { get; set; }
    public Func<WaveePlayerMediaItem, IComparable> KeySelector { get; set; }
}