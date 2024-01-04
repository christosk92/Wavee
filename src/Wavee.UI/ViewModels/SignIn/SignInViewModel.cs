using System.Reactive;
using ReactiveUI;
using Wavee.Interfaces;
using Wavee.Spotify;
using Wavee.UI.Query.Contracts;
using Wavee.UI.ViewModels.Profile;

namespace Wavee.UI.ViewModels.SignIn;

public sealed class SignInViewModel : ViewModelBase
{
    private readonly IWaveeSpotifyClient _waveeSpotifyClient;
    private ProfileViewModel? _lastProfile;
    private Exception? _lastError;
    private TaskCompletionSource<string>? _oauthTcs;

    public SignInViewModel(
        IWaveePlayer player,
        WaveeSpotifyConfig config)
    {
        _waveeSpotifyClient = WaveeSpotifyClient.Create(
            player: player,
            config: config,
            oAuthCallbackDelegate: OAuthCallbackDelegate
        );

        Authenticate = ReactiveCommand.CreateFromTask(AuthenticateTask);
        NavigatedToUrl = ReactiveCommand.Create<string>(NavigatedToUrlFunc);
    }

    public ProfileViewModel? LastProfile
    {
        get => _lastProfile;
        private set => this.RaiseAndSetIfChanged(ref _lastProfile, value);
    }

    public Exception? LastError
    {
        get => _lastError;
        private set => this.RaiseAndSetIfChanged(ref _lastError, value);
    }

    public ReactiveCommand<Unit, Unit> Authenticate { get; }
    public ReactiveCommand<string, Unit> NavigatedToUrl { get; }

    public event EventHandler<ProfileContext> ProfileAuthenticated;
    public event EventHandler<Exception> AuthenticationFailed;
    public event EventHandler<string> UrlNavigationRequested;

    private async Task<Unit> AuthenticateTask(CancellationToken arg)
    {
        try
        {
            var connected = await _waveeSpotifyClient.Remote.Connect(arg);
            if (connected)
            {
                var profileContext = ProfileContext.Spotify(_waveeSpotifyClient);
                LastProfile = await profileContext.Query(new GetProfileInformationQuery());
                ProfileAuthenticated?.Invoke(this, profileContext);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            LastError = e;
            AuthenticationFailed?.Invoke(this, e);
            throw;
        }

        return Unit.Default;
    }

    private Task<string> OAuthCallbackDelegate(string url)
    {
        _oauthTcs = new TaskCompletionSource<string>();
        UrlNavigationRequested?.Invoke(this, url);
        return _oauthTcs.Task;
    }

    private void NavigatedToUrlFunc(string url)
    {
        // if url is the callback url
        if (url.StartsWith("http://127.0.0.1:5001/login"))
        {
            _oauthTcs?.SetResult(url);
        }
    }
}