using Wavee.ViewModels.Enums;
using Wavee.ViewModels.Interfaces;
using Wavee.ViewModels.Models;
using Wavee.ViewModels.Models.Items;
using Wavee.ViewModels.ViewModels.Common;
using Wavee.ViewModels.ViewModels.Library.Items;
using Wavee.ViewModels.ViewModels.NavBar;

namespace Wavee.ViewModels.ViewModels.Library.Components;

[NavigationMetaData(Title = "Liked Songs")]
public partial class LibrarySongsViewModel : AbsLibraryComponentViewModel<LibrarySongViewModel>
{
    public LibrarySongsViewModel(ILibraryService libraryService) : base(LibraryItemType.LikedSongs, libraryService)
    {
    }

    protected override LibrarySongViewModel Create(LibraryItem item)
    {
        var songViewModel = new SongViewModel((WaveeSongItem)item.Item);
        return new LibrarySongViewModel(item, songViewModel);
    }
}