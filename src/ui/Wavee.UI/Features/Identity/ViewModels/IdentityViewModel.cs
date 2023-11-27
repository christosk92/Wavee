using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Wavee.Spotify.Application.Authentication.Modules;
using Wavee.Spotify.Common.Contracts;
using Wavee.UI.Features.Identity.Entities;
using Wavee.UI.Features.Identity.Requests;

namespace Wavee.UI.Features.Identity.ViewModels;

public sealed class IdentityViewModel : ObservableRecipient, IRecipient<SpotifyOAuthRequest>
{
    private TaskCompletionSource<OpenBrowserResult>? _browserRequested;
    private string? _browserUrl;
    private WaveeUser? _user;
    private bool _isLoading;

    private readonly ISpotifyClient _spotifyClient;

    public IdentityViewModel(ISpotifyClient spotifyClient)
    {
        _spotifyClient = spotifyClient;
    }

    public async Task Initialize()
    {
        IsActive = true;
        IsLoading = true;

        var me = await _spotifyClient.Initialize();
        User = new WaveeUser();
        IsLoading = false;
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public string BrowserUrl
    {
        get => _browserUrl;
        private set => SetProperty(ref _browserUrl, value);
    }

    public WaveeUser? User
    {
        get => _user;
        private set => SetProperty(ref _user, value);
    }

    public void Receive(SpotifyOAuthRequest message)
    {
        IsLoading = false;
        BrowserUrl = message.Url;
        _browserRequested = message.BrowserRequested;
    }

    public void OnRedirect(string argsUri)
    {
        IsLoading = true;
        _browserRequested?.TrySetResult(new OpenBrowserResult(argsUri, true));
    }
}