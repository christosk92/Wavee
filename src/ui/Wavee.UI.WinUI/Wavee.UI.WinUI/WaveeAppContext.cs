using System;
using System.Linq;
using System.Threading.Tasks;
using ABI.System;
using Wavee.UI.Providers;
using Wavee.UI.Providers.Spotify;
using Wavee.UI.Services;
using Wavee.UI.ViewModels.Feed;
using Wavee.UI.ViewModels.Library;
using Wavee.UI.ViewModels.Login;
using Wavee.UI.ViewModels.NowPlaying;
using Wavee.UI.ViewModels.Playlists;
using Wavee.UI.ViewModels.Shell;
using Wavee.UI.WinUI.Windows;

namespace Wavee.UI.WinUI;

public sealed class WaveeAppContext
{
    private readonly IDispatcher _dispatcherWrapper;
    private readonly IWaveeUIProvider[] _providers;
    public WaveeAppContext(IDispatcher dispatcherWrapper, params IWaveeUIProvider[] providers)
    {
        _dispatcherWrapper = dispatcherWrapper;
        _providers = providers;
        SpotifyLoginViewModelFactory = (x) => new SpotifyLoginViewModel(dispatcherWrapper, x);
    }

    public async Task Initialize()
    {
        foreach (var authProvider in _providers)
        {
            authProvider.Authentication.AuthenticationRequested += AuthenticationOnAuthenticationRequested;
            authProvider.Authentication.AuthenticationDone += AuthenticationOnAuthenticationDone;

            await authProvider.Initialize();
        }
    }


    public Func<WaveeUIAuthenticationModule, SpotifyLoginViewModel> SpotifyLoginViewModelFactory { get; }
    public ShellViewModel? ShellViewModel { get; private set; }

    public event System.EventHandler<WaveeUIAuthenticationModule> AuthorizationRequested;
    public event System.EventHandler<IWaveeUIProvider> AuthorizationDone;

    private void AuthenticationOnAuthenticationRequested(object sender, WaveeUIAuthenticationModule e)
    {
        if (sender is not IWaveeUIAuthenticationProvider authprovider) return;

        var provider = authprovider.RootProvider;
        if (provider == null) return;

        _dispatcherWrapper?.Dispatch(() =>
        {
            if (authprovider.AuthenticatedProfile is not null)
            {
                ShellViewModel?.RemoveProfile(authprovider.AuthenticatedProfile);
            }
            AuthorizationRequested?.Invoke(this, e);
        });
    }

    private void AuthenticationOnAuthenticationDone(object sender, EventArgs e)
    {
        if (sender is not IWaveeUIAuthenticationProvider authprovider) return;

        var provider = authprovider.RootProvider;
        if (provider == null) return;

        _dispatcherWrapper?.Dispatch(() =>
        {
            if (ShellViewModel is null)
            {
                ShellViewModel = new ShellViewModel(
                    feed: new FeedViewModel(),
                    library: new LibraryRootViewModel(new LibraryTracksViewModel(_dispatcherWrapper),
                        new LibraryArtistsViewModel(_dispatcherWrapper),
                        new LibraryAlbumsViewModel(_dispatcherWrapper)),
                    nowPlaying: new NowPlayingViewModel(),
                    playlists: new PlaylistsViewModel(),
                    authprovider.AuthenticatedProfile!);
            }
            else
            {
                ShellViewModel.PrepareProfile(authprovider.AuthenticatedProfile!);
            }

            AuthorizationDone?.Invoke(this, provider);
        });
    }
}