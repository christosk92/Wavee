using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Wavee.Spotify.Application.Authentication.Modules;
using Wavee.Spotify.Common.Contracts;
using Wavee.UI.Features.Identity.Entities;
using Wavee.UI.Features.Identity.Requests;
using Wavee.UI.Features.Playback.Notifications;
using Wavee.UI.Features.Playback.ViewModels;

namespace Wavee.UI.Features.Identity.ViewModels;

public sealed class IdentityViewModel : ObservableRecipient, IRecipient<SpotifyOAuthRequest>
{
    private TaskCompletionSource<OpenBrowserResult>? _browserRequested;
    private string? _browserUrl;
    private WaveeUser? _user;
    private bool _isLoading;

    private readonly ISpotifyClient _spotifyClient;
    private readonly IMediator _mediator;
    private readonly Func<SpotifyRemotePlaybackPlayerViewModel> _spotifyRemotePlayerFactory;
    private readonly Func<LocalPlaybackPlayerviewModel> _localPlaybackPlayerFactory;
    public IdentityViewModel(ISpotifyClient spotifyClient, IServiceProvider provider, IMediator mediator)
    {
        _spotifyClient = spotifyClient;
        _mediator = mediator;
        _spotifyRemotePlayerFactory = provider.GetRequiredService<SpotifyRemotePlaybackPlayerViewModel>;
        _localPlaybackPlayerFactory = provider.GetRequiredService<LocalPlaybackPlayerviewModel>;
    }

    public async Task Initialize()
    {
        IsActive = true;
        IsLoading = true;

        var me = await _spotifyClient.Initialize();
        await _mediator.Publish(new PlaybackPlayerChangedNotification
        {
            Player = _spotifyRemotePlayerFactory()
        });
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