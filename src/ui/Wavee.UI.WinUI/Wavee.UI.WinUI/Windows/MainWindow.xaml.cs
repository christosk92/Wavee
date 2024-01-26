using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Wavee.Spfy;
using Wavee.UI.Providers;
using Wavee.UI.Providers.Spotify;
using Wavee.UI.Services;
using Wavee.UI.ViewModels.Login;
using Wavee.UI.WinUI.Views.Login;
using Wavee.UI.WinUI.Views.Shell;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace Wavee.UI.WinUI.Windows;

public sealed partial class MainWindow : Window
{
    private UIElement? _preContent;

    public MainWindow()
    {
        this.InitializeComponent();
        this.SystemBackdrop = new MicaBackdrop();
        this.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        this.AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;

        var context = CreateNewContext();
        context.AuthorizationRequested += ContextOnAuthorizationRequested;
        context.AuthorizationDone += ContextOnAuthorizationDone;

        Task.Run(async () => await context.Initialize());
    }

    private void ContextOnAuthorizationDone(object sender, IWaveeUIProvider e)
    {
        var ctx = (sender as WaveeAppContext);

        this.Content = _preContent ?? new ShellView(ctx!.ShellViewModel);
    }

    private void ContextOnAuthorizationRequested(object sender, WaveeUIAuthenticationModule module)
    {
        _preContent = this.Content;

        var ctx = (sender as WaveeAppContext);
        switch (module.RootProvider)
        {
            case WaveeUISpotifyProvider:
                {
                    var spotifyloginVm = ctx.SpotifyLoginViewModelFactory(module);
                    this.Content = new SpotifyLoginView(spotifyloginVm);
                    break;
                }
        }
    }

    private WaveeAppContext CreateNewContext()
    {
        var dispatcher = this.DispatcherQueue;
        var dispatcherWrapper = new DispatcherWrapper(dispatcher);
        var serviceProviders = new WaveeUISpotifyProvider(new WinUISecureStorage(), new WaveePlayer(null));
        return new WaveeAppContext(dispatcherWrapper, serviceProviders);
    }
}

internal sealed class WinUISecureStorage : ISecureStorage
{
    private string _defaultPath;

    public WinUISecureStorage()
    {
        _defaultPath = Path.Combine(AppContext.BaseDirectory, "default.txt");
    }

    private const string Resource = "WaveeDbg";
    public ValueTask Remove(string username)
    {
        try
        {
            var creds = new PasswordVault();
            var cred = creds.Retrieve(Resource, username);
            if (cred is not null)
            {
                creds.Remove(cred);
            }
            return ValueTask.CompletedTask;
        }
        catch (Exception ex)
        {
            return ValueTask.CompletedTask;
        }
    }

    public ValueTask Store(string username, string pwd)
    {
        try
        {
            var creds = new PasswordVault();
            creds.Add(new PasswordCredential(Resource, username, pwd));
            File.WriteAllText(_defaultPath, username);

            return ValueTask.CompletedTask;
        }
        catch (Exception ex)
        {
            return ValueTask.CompletedTask;
        }
    }

    public bool TryGetDefaultUser(out string userId)
    {
        if (File.Exists(_defaultPath))
        {
            userId = File.ReadAllText(_defaultPath);

            try
            {
                var creds = new PasswordVault();
                var item = creds.Retrieve(Resource, userId);
                if (item is not null)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                userId = default;
                return false;
            }
        }

        userId = default;
        return false;
    }

    public bool TryGetStoredCredentialsForUser(string userId, out string password)
    {
        try
        {
            var creds = new PasswordVault();
            var source = creds.Retrieve(Resource, userId);
            source.RetrievePassword();

            password = source.Password;
            return true;
        }
        catch (Exception ex)
        {
            password = null;
            return false;
        }
        password = null;
        return false;
    }
}

internal sealed class DispatcherWrapper : IDispatcher
{
    private readonly DispatcherQueue _dispatcherQueue;
    public DispatcherWrapper(DispatcherQueue dispatcherQueue)
    {
        _dispatcherQueue = dispatcherQueue;
    }

    public void Dispatch(Action action, bool highPriority = false)
    {
        _dispatcherQueue.TryEnqueue(
            highPriority ? DispatcherQueuePriority.High : DispatcherQueuePriority.Normal,
            () => action());
    }
}