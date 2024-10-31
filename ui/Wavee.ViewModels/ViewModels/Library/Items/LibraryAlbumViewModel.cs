using Wavee.ViewModels.Models;
using Wavee.ViewModels.ViewModels.Common;

namespace Wavee.ViewModels.ViewModels.Library.Items;

public class LibraryAlbumViewModel : LibraryItemViewModel
{
    public LibraryAlbumViewModel(LibraryItem libraryItem, AlbumViewModel album) : base(libraryItem, album)
    {
        
    }

    public AlbumViewModel Album => (AlbumViewModel)ViewModel;

    public override void Update(LibraryItem item)
    {
        throw new NotImplementedException();
    }
}