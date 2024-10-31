using Wavee.ViewModels.Interfaces;
using Wavee.ViewModels.ViewModels.Library.Components;

namespace Wavee.ViewModels.ViewModels.Library;

public sealed class LibrariesViewModel
{
    public LibrariesViewModel(ILibraryService libraryService)
    {
        Albums = new LibraryAlbumsViewModel(libraryService);
        Artists = new LibraryArtistsViewModel(libraryService);
        Songs = new LibrarySongsViewModel(libraryService);
    }

    public LibraryAlbumsViewModel Albums { get; }
    public LibraryArtistsViewModel Artists { get; }
    public LibrarySongsViewModel Songs { get; }
}