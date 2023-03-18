using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Wavee.Enums;
using Wavee.UI.Interfaces.Services;
using Wavee.UI.Interfaces.ViewModels;
using Wavee.UI.Models.Navigation;

namespace Wavee.UI.ViewModels.Libray
{
    public sealed partial class LibraryRootViewModel : ObservableObject, INavigationAware
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedViewIndex))]
        private AbsLibraryViewModel? _currentView;

        private readonly IServiceScope _scope;
        private readonly INavigationService _navigationService;
        public LibraryRootViewModel(IServiceProvider serviceProvider, INavigationService navigationService)
        {
            _navigationService = navigationService;
            _scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        }

        public int SelectedViewIndex
        {
            get => _currentView switch
            {
                LibraryAlbumsViewModel => 0,
                LibrarySongsViewModel => 1,
                LibraryArtistsViewModel => 2,
                _ => 0
            };
            set
            {
                bool prevHearted = false;
                ServiceType prevService = ServiceType.Local;
                if (CurrentView != null)
                {
                    prevHearted = CurrentView.HeartedFilter;
                    prevService = CurrentView.Service!.Value;
                }

                switch (value)
                {
                    case 0:
                        CurrentView = _scope.ServiceProvider.GetRequiredService<LibraryAlbumsViewModel>();
                        _navigationService.InvokePseudoNavigation(new SharedNavigationEventArgs(typeof(LibraryRootViewModel),
                            new LibraryNavigationParameters(nameof(LibraryAlbumsViewModel), prevHearted, prevService)));
                        break;
                    case 1:
                        CurrentView = _scope.ServiceProvider.GetRequiredService<LibrarySongsViewModel>();
                        _navigationService.InvokePseudoNavigation(new SharedNavigationEventArgs(typeof(LibraryRootViewModel),
                            new LibraryNavigationParameters(nameof(LibrarySongsViewModel), prevHearted, prevService))); break;
                    case 2:
                        CurrentView = _scope.ServiceProvider.GetRequiredService<LibraryArtistsViewModel>();
                        _navigationService.InvokePseudoNavigation(new SharedNavigationEventArgs(typeof(LibraryRootViewModel),
                            new LibraryNavigationParameters(nameof(LibraryArtistsViewModel), prevHearted, prevService))); break;
                    default:
                        CurrentView = null;
                        break;
                }

                if (CurrentView != null)
                {
                    CurrentView.HeartedFilter = prevHearted;
                    CurrentView.Service = prevService;
                    _ = CurrentView.Initialize();
                }
            }
        }

        public async void OnNavigatedTo(object parameter)
        {
            if (parameter is LibraryNavigationParameters navParameters)
            {
                AbsLibraryViewModel? library = navParameters.NavigateTo switch
                {
                    nameof(LibrarySongsViewModel) => _scope.ServiceProvider.GetRequiredService<LibrarySongsViewModel>(),
                    nameof(LibraryAlbumsViewModel) =>
                        _scope.ServiceProvider.GetRequiredService<LibraryAlbumsViewModel>(),
                    nameof(LibraryArtistsViewModel) => _scope.ServiceProvider
                        .GetRequiredService<LibraryArtistsViewModel>(),
                    _ => null
                };

                if (library != null)
                {
                    library.Service = navParameters.ForService;
                    _currentView = library;
                    await library.Initialize();
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public void OnNavigatedFrom()
        {
            _scope.Dispose();
        }
    }
}
