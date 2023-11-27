using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Wavee.Spotify.Application.Authentication.Modules;
using Wavee.UI.Features.Identity.Requests;
using Wavee.UI.Features.Identity.ViewModels;
using Wavee.UI.Features.Shell.ViewModels;

namespace Wavee.UI.Features.MainWindow;

public sealed class MainWindowViewModel : ObservableRecipient
{
    public MainWindowViewModel(IdentityViewModel identity, ShellViewModel shell)
    {
        Identity = identity;
        Shell = shell;
    }

    public IdentityViewModel Identity { get; }
    public ShellViewModel Shell { get; }

    public void RequestOpenBrowser(string url, TaskCompletionSource<OpenBrowserResult> tcs)
    {
        Messenger.Send(new SpotifyOAuthRequest
        {
            Url = url,
            BrowserRequested = tcs
        });
    }
}