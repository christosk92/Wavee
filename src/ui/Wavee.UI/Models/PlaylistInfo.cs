using Wavee.Core.Ids;

namespace Wavee.UI.Models;

public readonly struct PlaylistInfo
{
    public PlaylistInfo(AudioId Id, int Index, string Name)
    {
        this.Id = Id;
        this.Index = Index;
        this.Name = Name;
    }

    public AudioId Id { get;  }
    public int Index { get; }
    public string Name { get; }

    public void Deconstruct(out AudioId Id, out int Index, out string Name)
    {
        Id = this.Id;
        Index = this.Index;
        Name = this.Name;
    }
}