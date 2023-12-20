using CommunityToolkit.Mvvm.Input;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Wavee.Spotify.Common;
using Wavee.UI.Features.Album.ViewModels;
using Wavee.UI.Features.Artist.ViewModels;
using Wavee.UI.Features.Navigation;
using Wavee.UI.Features.Playback.ViewModels;
using Wavee.UI.Features.Playlists.ViewModel;
using Wavee.UI.Features.RightSidebar.ViewModels;
using Wavee.UI.Test;

namespace Wavee.UI.Extensions;

public static class NavigationExtensions
{
    public static void NavigateToId(this INavigationService service, string id)
    {
        if (SpotifyId.TryParse(id, out var spId))
        {
            switch (spId.Type)
            {
                case SpotifyItemType.Artist:
                    service.NavigateToArtist(id);
                    break;
                case SpotifyItemType.Album:
                    service.NavigateToAlbum(id);
                    break;
            }
        }
    }

    private static void NavigateToArtist(this INavigationService service, string id)
    {
        var mediator = Constants.ServiceProvider.GetRequiredService<IMediator>();
        var dispatcher = Constants.ServiceProvider.GetRequiredService<IUIDispatcher>();
        var playback = Constants.ServiceProvider.GetRequiredService<PlaybackViewModel>();
        var vm = new ArtistViewModel(mediator, id, dispatcher, playback);

        service.Navigate(null, vm);
    }

    private static void NavigateToAlbum(this INavigationService service, string id)
    {
        var mediator = Constants.ServiceProvider.GetRequiredService<IMediator>();
        var playback = Constants.ServiceProvider.GetRequiredService<PlaybackViewModel>();
        var vm = new AlbumViewViewModel(id,mediator, playback);

        service.Navigate(null, vm);
    }
}