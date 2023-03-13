using CommunityToolkit.Mvvm.ComponentModel;
using Wavee.UI.Identity.Users.Contracts;
using Wavee.UI.Models;

namespace Wavee.UI.ViewModels.AudioItems;

public partial class AlbumViewModel : ObservableRecipient,
    IPlayableItem,
    IAddableItem,
    IEditableItem,
    IDescribeable
{
    public AlbumViewModel(IAlbum localAlbum)
    {
        Album = localAlbum;
    }
    public IAlbum Album
    {
        get;
    }

    public bool IsNull(string? s, bool ifNull)
    {
        return string.IsNullOrEmpty(s) ? ifNull : !ifNull;
    }


    public bool CanEdit => Album.ServiceType is ServiceType.Local;
    public string Describe() => Album.Name;
}

public interface IEditableItem
{
    bool CanEdit
    {
        get;
    }
}

public interface IPlayableItem
{

}

public interface IAddableItem
{

}

public interface IDescribeable
{
    string Describe();
}