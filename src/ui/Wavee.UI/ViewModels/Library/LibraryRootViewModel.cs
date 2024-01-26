using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wavee.UI.Providers;
using Wavee.UI.Services;

namespace Wavee.UI.ViewModels.Library;

public sealed class LibraryRootViewModel : ObservableObject, IHasProfileViewModel
{
    private ILibraryComponentViewModel? _currentComponent;

    public LibraryRootViewModel(LibraryTracksViewModel tracks, LibraryArtistsViewModel artists, LibraryAlbumsViewModel albums)
    {
        RootNavigationItems = [tracks, artists, albums];

        RootNavigationClickedCommand = new RelayCommand<object>(NavigateClicked);
    }

    public void SetNavigationController(INavigationController navigation)
    {
        NavigationController?.Dispose();
        NavigationController = null;
        GC.Collect();

        NavigationController = navigation;

        if (CurrentComponent is not null)
        {
            NavigationController.NavigateTo(CurrentComponent);
        }
    }
    public INavigationController? NavigationController { get; private set; }
    public IReadOnlyCollection<ILibraryComponentViewModel> RootNavigationItems { get; }
    public ICommand RootNavigationClickedCommand { get; }

    public ILibraryComponentViewModel? CurrentComponent
    {
        get => _currentComponent;
        set => this.SetProperty(ref _currentComponent, value);
    }

    public void AddFromProfile(IWaveeUIAuthenticatedProfile profile)
    {
        foreach (var item in RootNavigationItems)
        {
            item.AddFromProfile(profile);
        }
    }

    public void RemoveFromProfile(IWaveeUIAuthenticatedProfile profile)
    {
        foreach (var item in RootNavigationItems)
        {
            item.RemoveFromProfile(profile);
        }
    }

    private void NavigateClicked(object? obj)
    {
        CurrentComponent = obj as ILibraryComponentViewModel;
        NavigationController?.NavigateTo(obj);
    }
}