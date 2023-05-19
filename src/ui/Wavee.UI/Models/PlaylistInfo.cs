using LanguageExt;
using Wavee.Core.Ids;

namespace Wavee.UI.Models;

public record PlaylistInfo
{
    public PlaylistInfo(string Id,
        int Index,
        string Name,
        string OwnerId,
        bool IsFolder,
        Seq<PlaylistInfo> SubItems,
        DateTimeOffset Timestamp, bool isInFolder)
    {
        this.Id = Id;
        this.Index = Index;
        this.Name = Name;
        this.OwnerId = OwnerId;
        this.IsFolder = IsFolder;
        this.SubItems = SubItems;
        this.Timestamp = Timestamp;
        IsInFolder = isInFolder;
    }

    public string Id { get; }
    public int Index { get; }
    public string Name { get; }
    public string OwnerId { get; }
    public bool IsFolder { get; }
    public Seq<PlaylistInfo> SubItems { get; }
    public DateTimeOffset Timestamp { get; }
    public bool IsInFolder { get; }


    public PlaylistInfo AddSubitem(PlaylistInfo playlistInfo)
    {
        var newSubitems = SubItems.Add(playlistInfo);
        return new PlaylistInfo(Id, Index, Name, OwnerId, IsFolder, newSubitems, Timestamp, false);
    }
}