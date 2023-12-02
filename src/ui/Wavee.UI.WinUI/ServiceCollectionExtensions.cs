using System;
using Microsoft.Extensions.DependencyInjection;
using Wavee.UI.Features.Navigation;
using Wavee.UI.Test;
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
        collection.AddSingleton<IUIDispatcher, WinUIDispatcher>();
        return collection;
    }
}

public sealed class WinUIDispatcher : IUIDispatcher
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;
    public WinUIDispatcher()
    {
        _dispatcher = App.MainWindow.DispatcherQueue;

    }
    public void Invoke(Action action)
    {
        _dispatcher.TryEnqueue(() => action());
    }
}