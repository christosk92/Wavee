using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Eum.Spotify.connectstate;
using Google.Protobuf.Collections;
using ReactiveUI;
using Wavee.Core;
using Wavee.Spotify.Models.Common;
using Wavee.Spotify.Models.Interfaces;
using Wavee.Spotify.Playback;

namespace Wavee.Spotify.Models.Response;

/// <summary>
/// A controllable device that is connected to Spotify Remote.
///
/// You may use this class to control the device's state and playback.
/// 
/// </summary>
public sealed class SpotifyPrivateDevice : INotifyPropertyChanged, IDisposable
{
    private Func<Task>? _onManualClose;
    private string _deviceName;
    private DeviceType _deviceType;
    private readonly CompositeDisposable _disposables;
    private ISpotifyClient? _parentClient;

    internal SpotifyPrivateDevice(
        ISpotifyClient parentClient,
        string deviceId,
        string deviceName,
        DeviceType deviceType,
        IObservable<Cluster> clusterChanged,
        IObservable<WaveePlaybackState> localPlaybackStateChanged,
        Func<string, DeviceType, PutStateReason, PlayerState, CancellationToken, Task> updateState,
        Func<Task> onManualClose)
    {
        _onManualClose = onManualClose;
        _parentClient = parentClient;
        _disposables = new CompositeDisposable();
        DeviceId = deviceId;

        CurrentlyPlaying = clusterChanged
            .CombineLatest(localPlaybackStateChanged,
                (cluster, playbackState) => new { Cluster = cluster, PlaybackState = playbackState })
            .SelectMany(async x =>
            {
                if (x.PlaybackState is { IsActive: true, Source: SpotifyMediaSource spotifyMediaSource })
                {
                    return await ConstructFromLocalPlaybackState(x.PlaybackState, spotifyMediaSource, x.Cluster.Device);
                }

                return await ConstructFromRemoteCluster(x.Cluster);
            });

        IsActive = clusterChanged
            .Select(x => x.ActiveDeviceId == deviceId);

        DeviceName = deviceName;
        DeviceType = deviceType;

        this.WhenAnyValue(x => x.DeviceName, x => x.DeviceType)
            .Skip(1)
            .SelectMany(async x =>
            {
                var (newDeviceName, newDeviceType) = x;
                var state = BuildState();
                await updateState(newDeviceName, newDeviceType, PutStateReason.PlayerStateChanged, state,
                    CancellationToken.None);
                return Unit.Default;
            })
            .Throttle(TimeSpan.FromMilliseconds(400))
            .Subscribe();
    }

    /// <summary>
    /// An observable that emits the currently playing state the spotify system.
    ///
    /// If the device is not playing a spotify track, this observable will emit the remote's state.
    /// </summary>
    public IObservable<SpotifyCurrentlyPlaying> CurrentlyPlaying { get; }

    /// <summary>
    /// An observable that emits whether the device is currently active.
    ///
    /// Active means that the device is currently spotify music.
    /// </summary>
    public IObservable<bool> IsActive { get; }

    public event EventHandler? Closed;
    public string DeviceId { get; }

    public string DeviceName
    {
        get => _deviceName;
        set => SetField(ref _deviceName, value);
    }

    public DeviceType DeviceType
    {
        get => _deviceType;
        set => SetField(ref _deviceType, value);
    }

    private PlayerState BuildState()
    {
        throw new NotImplementedException();
    }

    private async Task<SpotifyCurrentlyPlaying> ConstructFromRemoteCluster(Cluster cluster)
    {
        var timestamp = cluster.PlayerState.Timestamp;
        var posSinceTs = cluster.PlayerState.PositionAsOfTimestamp;
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var diff = now - timestamp;
        var pos = TimeSpan.FromMilliseconds(diff + posSinceTs);

        ISpotifyPlayableItem? item = null;
        if (!string.IsNullOrEmpty(cluster.PlayerState?.Track?.Uri))
        {
            item = await _parentClient!.Cache.TryGetOrFetch<ISpotifyPlayableItem>(cluster.PlayerState.Track.Uri,
                async (id, token) =>
                {
                    if (SpotifyId.TryParse(id, out var spotifyId))
                    {
                        switch (spotifyId.Type)
                        {
                            case AudioItemType.Track:
                                return await _parentClient.Tracks.Get(spotifyId, token);
                            case AudioItemType.PodcastEpisode:
                                return await _parentClient.Episodes.Get(spotifyId, token);
                        }
                    }

                    return null;
                }, CancellationToken.None);
        }

        SpotifyContextInfo? context = null;
        if (!string.IsNullOrEmpty(cluster.PlayerState?.ContextUri))
        {
            var ctxUri = cluster.PlayerState.ContextUri;
            var ctxUrl = cluster.PlayerState.ContextUrl;
            SpotifyContextTrackInfo trackInfo = new();
            if (cluster.PlayerState.Index is not null)
            {
                trackInfo = trackInfo with
                {
                    PageIndex = cluster.PlayerState.Index.Page,
                    TrackIndex = cluster.PlayerState.Index.Track,
                };
            }

            if (!string.IsNullOrEmpty(cluster.PlayerState?.Track?.Uri) &&
                SpotifyId.TryParse(cluster.PlayerState?.Track?.Uri, out var spotifyId))
            {
                trackInfo = trackInfo with
                {
                    TrackId = spotifyId,
                };
            }

            if (!string.IsNullOrEmpty(cluster.PlayerState?.Track?.Uid))
            {
                trackInfo = trackInfo with
                {
                    TrackUid = cluster.PlayerState.Track.Uid,
                };
            }

            var metadata = cluster.PlayerState?.ContextMetadata?.ToDictionary() ?? new Dictionary<string, string>();
            context = new SpotifyContextInfo(ctxUri, ctxUrl, metadata, trackInfo);
        }

        return new SpotifyCurrentlyPlaying(pos)
        {
            IsPlayingOnThisDevice = false,
            DeviceId = cluster.ActiveDeviceId,
            Paused = string.IsNullOrEmpty(cluster.ActiveDeviceId) || cluster.PlayerState?.IsPaused is true,
            ShuffleState = cluster.PlayerState?.Options?.ShufflingContext is true,
            RepeatState = cluster.PlayerState?.Options?.RepeatingContext is true
                ? RepeatState.Context
                : (cluster.PlayerState?.Options?.RepeatingTrack is true
                    ? RepeatState.Track
                    : RepeatState.None),
            Devices = MapDevices(cluster.Device),
            IsActive = !string.IsNullOrEmpty(cluster.ActiveDeviceId),
            Item = item,
            Context = context,
        };
    }

    private async Task<SpotifyCurrentlyPlaying> ConstructFromLocalPlaybackState(WaveePlaybackState playbackState,
        SpotifyMediaSource spotifyMediaSource,
        MapField<string, DeviceInfo> device)
    {
        return new SpotifyCurrentlyPlaying(playbackState.PositionSinceStartStopwatch, playbackState.PositionStopwatch)
        {
            IsPlayingOnThisDevice = true,
            DeviceId = DeviceId,
            Paused = !playbackState.IsActive || playbackState.Paused,
            ShuffleState = playbackState.ShuffleState,
            RepeatState = playbackState.RepeatState,
            Devices = MapDevices(device),
            IsActive = playbackState.IsActive,
            Item = null,
            Context = null,
        };
    }

    private IReadOnlyDictionary<string, SpotifyDevice> MapDevices(MapField<string, DeviceInfo> device)
    {
        return device
            .Where(x => x.Key != DeviceId)
            .ToDictionary(x => x.Key, x => new SpotifyDevice(
                Id: x.Key,
                Name: x.Value.Name,
                Type: x.Value.DeviceType,
                Volume: x.Value.Capabilities.DisableVolume ? ((float?)null) : x.Value.Volume / (float)ushort.MaxValue
            ));
    }

    public async ValueTask Close()
    {
        if (_onManualClose is not null)
        {
            await _onManualClose();
            _onManualClose = null;
            Closed?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        Task.Run(async () => { await Close(); }).Wait();
        _disposables.Dispose();
        _parentClient = null;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}