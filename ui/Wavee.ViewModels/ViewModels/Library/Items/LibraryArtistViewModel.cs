using Wavee.ViewModels.Models;
using Wavee.ViewModels.ViewModels.Common;

namespace Wavee.ViewModels.ViewModels.Library.Items;

public class LibraryArtistViewModel : LibraryItemViewModel
{
    public LibraryArtistViewModel(LibraryItem libraryItem, ArtistViewModel artistViewmodel) : base(libraryItem, artistViewmodel)
    {
        
    }
    
    public ArtistViewModel Artist => (ArtistViewModel)ViewModel;

    public override void Update(LibraryItem item)
    {
        throw new NotImplementedException();
    }
}