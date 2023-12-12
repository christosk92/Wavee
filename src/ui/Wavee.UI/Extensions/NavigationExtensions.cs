using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Wavee.UI.Features.Artist.ViewModels;
using Wavee.UI.Features.Navigation;
using Wavee.UI.Test;

namespace Wavee.UI.Extensions;

public static class NavigationExtensions
{
    public static void NavigateToArtist(this INavigationService service, string id)
    {
        var mediator = Constants.ServiceProvider.GetRequiredService<IMediator>();
        var dispatcher = Constants.ServiceProvider.GetRequiredService<IUIDispatcher>();
        var vm = new ArtistViewModel(mediator, id, dispatcher);

        service.Navigate(null, vm);
    }
}