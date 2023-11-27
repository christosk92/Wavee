using Microsoft.Extensions.DependencyInjection;
using Wavee.UI.Features.Navigation;
using Wavee.UI.WinUI.Services;
using Wavee.UI.WinUI.Views.Libraries;
using Wavee.UI.WinUI.Views.Listen;
using Wavee.UI.WinUI.Views.NowPlaying;

namespace Wavee.UI.WinUI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddViews(this IServiceCollection collection)
    {
        collection.AddTransient<MainWindow>();
        collection.AddTransient<ListenPage>();

        collection.AddTransient<SongLibraryPage>();
        collection.AddTransient<AlbumLibraryPage>();
        collection.AddTransient<ArtistLibraryPage>();
        collection.AddTransient<PodcastLibraryPage>();

        collection.AddTransient<NowPlayingPage>();


        collection.AddSingleton<INavigationService, WinUINavigationService>();
        return collection;
    }
}