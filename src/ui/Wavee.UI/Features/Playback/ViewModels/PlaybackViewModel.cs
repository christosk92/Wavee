using System.Collections.ObjectModel;
using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using Mediator;
using Wavee.Domain.Playback;
using Wavee.Spotify.Common.Contracts;
using Wavee.Spotify.Domain.Remote;
using Wavee.Spotify.Utils;
using Wavee.UI.Domain.Playback;
using Wavee.UI.Features.Album.ViewModels;
using Wavee.UI.Features.Artist.Queries;
using Wavee.UI.Features.Dialog.Queries;
using Wavee.UI.Features.Navigation;
using Wavee.UI.Features.Playback.Notifications;

namespace Wavee.UI.Features.Playback.ViewModels;

public sealed class PlaybackViewModel : ObservableObject,
    INotificationHandler<PlaybackPlayerChangedNotification>
{
    private readonly IMediator _mediator;
    private readonly INavigationService _navigationService;
    private PlaybackPlayerViewModel? _activePlayer;
    private readonly ISpotifyClient _spotifyClient;
    public PlaybackViewModel(IMediator mediator,
        ISpotifyClient spotifyClient,
        INavigationService navigationService)
    {
        _mediator = mediator;
        _spotifyClient = spotifyClient;
        _navigationService = navigationService;

        var ownDeviceAsSpotifyDevice = new SpotifyDevice(
            Id: _spotifyClient.Config.Remote.DeviceId,
            Type: _spotifyClient.Config.Remote.DeviceType,
            Name: _spotifyClient.Config.Remote.DeviceName,
            Volume: null, // TODO
            Metadata: new Dictionary<string, string>()
        );
        OwnDevice = ownDeviceAsSpotifyDevice.ToRemoteDevice();
    }

    public PlaybackPlayerViewModel? ActivePlayer
    {
        get => _activePlayer;
        set
        {
            if (SetProperty(ref _activePlayer, value))
            {
                value?.Activate();
            }
        }
    }
    public event EventHandler PlaybackStateChanged;
    public ObservableCollection<PlaybackPlayerViewModel> Players { get; } = new();
    public RemoteDevice OwnDevice { get; }
    public ObservableCollection<RemoteDevice> Devices { get; } = new();

    public ValueTask Handle(PlaybackPlayerChangedNotification notification, CancellationToken cancellationToken)
    {
        if (ActivePlayer is not null)
        {
            Players.Add(ActivePlayer);
            ActivePlayer.PlaybackChanged -= ActivePlayerOnPlaybackChanged;
            ActivePlayer.DevicesChanged -= ActivePlayerOnDevicesChanged;
            ActivePlayer?.Dispose();
        }


        ActivePlayer = notification.Player;
        Devices.Clear();
        foreach (var device in ActivePlayer.Devices)
        {
            Devices.Add(device);
        }
        ActivePlayer.PlaybackChanged += ActivePlayerOnPlaybackChanged;
        ActivePlayer.DevicesChanged += ActivePlayerOnDevicesChanged;
        Players.Add(notification.Player);
        return ValueTask.CompletedTask;
    }

    private void ActivePlayerOnDevicesChanged(object? sender, RemoteDeviceChangeNotification e)
    {
        Devices.Clear();
        foreach (var device in e.Devices)
        {
            Devices.Add(device);
        }
    }

    private void ActivePlayerOnPlaybackChanged(object? sender, EventArgs e)
    {
        PlaybackStateChanged?.Invoke(this, EventArgs.Empty);

        if (_navigationService.CurrentViewModel is IPlaybackChangedListener listener)
        {
            listener.OnPlaybackChanged(this);
        }
    }

    public async Task Play(PlayContext ctx)
    {
        try
        {
            if (ActivePlayer is SpotifyRemotePlaybackPlayerViewModel remote
                && !string.IsNullOrEmpty(remote.Device?.Id))
            {
                // Do command
                try
                {
                    await _spotifyClient.Remote.Play(ctx._spContext, ctx._playOrigin, ctx._playOptions);
                    return;
                }
                catch (HttpRequestException req)
                {
                    if (req.StatusCode is not HttpStatusCode.NotFound)
                        throw;
                }
            }

            //play locally, but first prompt user to select device or play on this device
            var result = await _mediator.Send(new PromptDeviceSelectionQuery());
            switch (result.ResultType)
            {
                case PromptDeviceSelectionResultType.PlayOnDevice:
                {

                    await _spotifyClient.Remote.Play(ctx._spContext,
                        ctx._playOrigin,
                        ctx._playOptions,
                        overrideDeviceId: result.DeviceId);

                    break;
                }
                case PromptDeviceSelectionResultType.PlayOnThisDevice:
                {
                    //TODO:
                    break;
                }
                case PromptDeviceSelectionResultType.Nothing:
                {
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    public WaveeTrackPlaybackState IsPlaying(string id, string? uid)
    {
        if (_activePlayer is null)
            return WaveeTrackPlaybackState.NotPlaying;

        bool isplayingTrack = false;
        if (!string.IsNullOrEmpty(uid))
        {
            //TODO:
            isplayingTrack = false;
        }
        else
        {
            var state = ActivePlayer?.Id;
            isplayingTrack = state == id;
        }

        if (isplayingTrack)
        {
            return _activePlayer.IsPaused ? WaveeTrackPlaybackState.Paused : WaveeTrackPlaybackState.Playing;
        }

        return WaveeTrackPlaybackState.NotPlaying;
    }
}
