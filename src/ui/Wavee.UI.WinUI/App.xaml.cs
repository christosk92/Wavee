using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using LanguageExt.Pipes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Wavee.Spotify.Application.Authentication.Modules;
using Wavee.UI.WinUI.Interop;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Wavee.UI.Features.MainWindow;

namespace Wavee.UI.WinUI;

public partial class App : Microsoft.UI.Xaml.Application
{
    private static IServiceProvider _sp = null!;
    private static MainWindow _mWindow;

    public App()
    {
        this.InitializeComponent();

        _sp = new ServiceCollection()
            .AddWaveeUI(OpenBrowser, ApplicationData.Current.LocalFolder.Path)
            .AddViewModels()
            .AddViews()
            .AddMediator()
            .BuildServiceProvider();
        Constants.ServiceProvider = _sp;
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _mWindow = _sp.GetRequiredService<MainWindow>();
        _mWindow.SetViewModel(_sp.GetRequiredService<MainWindowViewModel>());
        _mWindow.Activate();
    }

    private static Task<OpenBrowserResult> OpenBrowser(string url, CancellationToken cancellationtoken)
    {
        var tcs = new TaskCompletionSource<OpenBrowserResult>();
        _mWindow.DispatcherQueue.TryEnqueue(() =>
        {
            MainWindow.ViewModel.RequestOpenBrowser(url, tcs);
        });

        return tcs.Task;
    }
    public static MainWindow MainWindow => _mWindow;
}