using Wavee.ViewModels.Models;
using Wavee.ViewModels.ViewModels.Common;

namespace Wavee.ViewModels.ViewModels.Library.Items;

public class LibrarySongViewModel : LibraryItemViewModel
{
    public LibrarySongViewModel(LibraryItem libraryItem, SongViewModel songViewmodel) : base(libraryItem, songViewmodel)
    {
        
    }

    public SongViewModel Song => (SongViewModel)ViewModel;

    public override void Update(LibraryItem item)
    {
        throw new NotImplementedException();
    }
}