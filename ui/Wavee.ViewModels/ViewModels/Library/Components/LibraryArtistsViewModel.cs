using Wavee.ViewModels.Enums;
using Wavee.ViewModels.Interfaces;
using Wavee.ViewModels.Models;
using Wavee.ViewModels.ViewModels.Common;
using Wavee.ViewModels.ViewModels.Library.Items;
using Wavee.ViewModels.ViewModels.NavBar;

namespace Wavee.ViewModels.ViewModels.Library.Components;

[NavigationMetaData(Title = "Artists")]
public partial class LibraryArtistsViewModel : AbsLibraryComponentViewModel<LibraryArtistViewModel>
{
    public LibraryArtistsViewModel(ILibraryService libraryService) : base(LibraryItemType.FollowedArtists,
        libraryService)
    {
    }

    protected override LibraryArtistViewModel Create(LibraryItem item)
    {
        var artistViewModel = new ArtistViewModel(item.Item);
        return new LibraryArtistViewModel(item, artistViewModel);
    }
}