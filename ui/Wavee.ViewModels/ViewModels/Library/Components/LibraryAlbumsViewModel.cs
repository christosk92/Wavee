using Wavee.ViewModels.Enums;
using Wavee.ViewModels.Interfaces;
using Wavee.ViewModels.Models;
using Wavee.ViewModels.ViewModels.Common;
using Wavee.ViewModels.ViewModels.Library.Items;
using Wavee.ViewModels.ViewModels.NavBar;

namespace Wavee.ViewModels.ViewModels.Library.Components;

[NavigationMetaData(Title = "Albums")]
public partial class LibraryAlbumsViewModel : AbsLibraryComponentViewModel<LibraryAlbumViewModel>
{
    public LibraryAlbumsViewModel(ILibraryService libraryService) : base(LibraryItemType.SavedAlbums, libraryService)
    {
    }

    protected override LibraryAlbumViewModel Create(LibraryItem item)
    {
        var albumViewModel = new AlbumViewModel(item.Item);
        return new LibraryAlbumViewModel(item, albumViewModel);
    }
}