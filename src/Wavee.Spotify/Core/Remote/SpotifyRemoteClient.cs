using System.Collections.Immutable;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Eum.Spotify.connectstate;
using Google.Protobuf.Collections;
using NeoSmart.AsyncLock;
using Tango.Types;
using Wavee.Core.Enums;
using Wavee.Core.Models;
using Wavee.Interfaces;
using Wavee.Spotify.Core.Mappings;
using Wavee.Spotify.Core.Models.Common;
using Wavee.Spotify.Core.Models.Playback;
using Wavee.Spotify.Infrastructure.Playback;
using Wavee.Spotify.Interfaces;
using Wavee.Spotify.Interfaces.Clients;
using Wavee.Spotify.Interfaces.Models;

namespace Wavee.Spotify.Core.Remote;

internal sealed class SpotifyRemoteClient : ISpotifyRemoteClient
{
    private string? _activeConnectionId;
    private readonly IWebSocketService _webSocketService;
    private readonly IWaveePlayer _player;
    private readonly WaveeSpotifyConfig _config;
    private DateTimeOffset? _playingSince = null;
    private string? _sessionId;
    private readonly Subject<Cluster> _state = new();
    private readonly ISpotifyMetadataClient _metadataClient;
    private readonly AsyncLock _notifyLock = new();

    public SpotifyRemoteClient(IWebSocketService webSocketService, IWaveePlayer player, WaveeSpotifyConfig config,
        ISpotifyMetadataClient metadataClient)
    {
        _webSocketService = webSocketService;
        _player = player;
        _config = config;
        _metadataClient = metadataClient;

        _player.PlaybackChanged += PlayerOnPlaybackChanged;
        _webSocketService.ClusterChanged += async (sender, cluster) =>
        {
            if(cluster.Cluster is null 
            || string.IsNullOrEmpty(cluster.Cluster.ActiveDeviceId)
            || cluster.Cluster.ActiveDeviceId != _config.Remote.DeviceId)
            {
                using (await _notifyLock.LockAsync())
                {
                    if (_playingSince is not null)
                    {
                        await NotifyInactivity();
                    }
                }
            }
            _state.OnNext(cluster.Cluster);
        };
    }

    public async ValueTask<bool> Connect(CancellationToken cancellationToken = default)
    {
        var connectionId = await _webSocketService.ConnectAsync(cancellationToken: cancellationToken);
        if (_activeConnectionId != connectionId)
        {
            _activeConnectionId = connectionId;
            return true;
        }

        return false;
    }

    public IObservable<WaveeSpotifyRemoteState> State
    {
        get
        {
            IObservable<Cluster> baseState = _state;
            if (!string.IsNullOrEmpty(_activeConnectionId) && _webSocketService.LatestCluster is not null)
            {
                baseState = baseState.StartWith(_webSocketService.LatestCluster);
            }

            return baseState.SelectMany(MutateToRemoteState);
        }
    }

    private async void PlayerOnPlaybackChanged(object? sender, WaveePlaybackState e)
    {
        if (e.Track is SpotifyAudioStream spotify)
        {
            // update remote device
            await UpdateSpotifyRemote(e, spotify);
        }
        else
        {
            using (await _notifyLock.LockAsync())
            {
                if (_playingSince is not null)
                {
                    // We are not playing a Spotify track. Disconnect from remote device if connected.
                    await NotifyInactivity();
                }
            }
        }
    }

    private async Task UpdateSpotifyRemote(WaveePlaybackState waveePlaybackState, SpotifyAudioStream spotify)
    {
        if (_playingSince is null)
        {
            _playingSince = DateTimeOffset.UtcNow;
            _sessionId = Guid.NewGuid().ToString();
        }

        // This mainly means creating a PutStateRequest with the active device
        var spotifyRemoteState = waveePlaybackState.ToPlayerState(_player.Position,
            sessionId: _sessionId,
            spotify);

        var request = spotifyRemoteState.ToPutState(
            PutStateReason.PlayerStateChanged,
            volume: _player.Volume,
            playerPosition: _player.Position,
            hasBeenPlayingSince: _playingSince,
            now: DateTimeOffset.UtcNow,
            lastCommandSentBy: null,
            lastCommandId: null,
            _config.Remote);

        await _webSocketService.PutState(request, CancellationToken.None);
    }

    private async Task NotifyInactivity()
    {
        _playingSince = null;
        _sessionId = null;

        await _player.Stop();
        // TODO
    }

    private async Task<WaveeSpotifyRemoteState> MutateToRemoteState(Cluster cluster)
    {
        try
        {
            var item = Option<SpotifySimpleContextItem>.None();
            var context = Option<SpotifySimpleContext>.None();
            var restrictions =
                new Dictionary<SpotifyRestrictionAppliesForType,
                    ImmutableArray<Either<string, SpotifyKnownRestrictionType>>>();
            bool isPaused = false;
            WaveeRepeatStateType repeatState = WaveeRepeatStateType.None;
            bool isShuffling = false;
            TimeSpan offset = TimeSpan.Zero;

            if (cluster?.PlayerState is { } playerState)
            {
                if (!string.IsNullOrEmpty(playerState.Track?.Uri)
                    && SpotifyId.TryParse(playerState.Track.Uri, out var spotifyId))
                {
                    var fetchItem = await _metadataClient.GetItem(spotifyId, true);
                    item = new SpotifySimpleContextItem
                    {
                        Item = (fetchItem as ISpotifyPlayableItem)!,
                        Uid = !string.IsNullOrEmpty(playerState.Track.Uid)
                            ? playerState.Track.Uid
                            : Option<string>.None(),
                        ItemIndex = playerState.Index is { } index ? (int)index.Track : Option<int>.None(),
                        PageIndex = playerState.Index is { } pgIndex ? (int)pgIndex.Page : Option<int>.None()
                    };
                }

                if (playerState.ContextUri.StartsWith("spotify:user:") &&
                    playerState.ContextUri.EndsWith(":collection"))
                {
                    context = new SpotifySimpleContext
                    {
                        Item = Option<ISpotifyItem>.None(),
                        Uri = playerState.ContextUri,
                        Metadata = playerState.ContextMetadata
                    };
                }
                else if (!string.IsNullOrEmpty(playerState.ContextUri)
                         && SpotifyId.TryParse(playerState.ContextUri, out var contextId))
                {
                    if (contextId.Type is not AudioItemType.Playlist and not AudioItemType.Unknown)
                    {
                        var fetchContext = await _metadataClient.GetItem(contextId, true);
                        context = new SpotifySimpleContext
                        {
                            Item = new Option<ISpotifyItem>(fetchContext),
                            Uri = playerState.ContextUri,
                            Metadata = playerState.ContextMetadata
                        };
                    }
                    else
                    {
                        context = new SpotifySimpleContext
                        {
                            Item = Option<ISpotifyItem>.None(),
                            Uri = playerState.ContextUri,
                            Metadata = playerState.ContextMetadata
                        };
                    }
                }
                else if (!string.IsNullOrEmpty(playerState.ContextUri))
                {
                    context = new SpotifySimpleContext
                    {
                        Item = Option<ISpotifyItem>.None(),
                        Uri = playerState.ContextUri,
                        Metadata = playerState.ContextMetadata
                    };
                }

                isPaused = playerState.IsPaused;
                if (playerState.Options.RepeatingTrack)
                {
                    repeatState = WaveeRepeatStateType.Track;
                }
                else if (playerState.Options.RepeatingContext)
                {
                    repeatState = WaveeRepeatStateType.Context;
                }
                else
                {
                    repeatState = WaveeRepeatStateType.None;
                }

                isShuffling = playerState.Options.ShufflingContext;

                static ImmutableArray<Either<string, SpotifyKnownRestrictionType>> MutateTo(RepeatedField<string> f)
                {
                    return f.Select(x =>
                    {
                        return x switch
                        {
                            "not_paused" => new Either<string, SpotifyKnownRestrictionType>(
                                SpotifyKnownRestrictionType.NotPaused),
                            "endless_context" => new Either<string, SpotifyKnownRestrictionType>(
                                SpotifyKnownRestrictionType.EndlessContext),
                            "dj-disallow" => new Either<string, SpotifyKnownRestrictionType>(
                                SpotifyKnownRestrictionType.Dj),
                            "narration" => new Either<string, SpotifyKnownRestrictionType>(SpotifyKnownRestrictionType
                                .Narration),
                            "no_prev_track" => new Either<string, SpotifyKnownRestrictionType>(
                                SpotifyKnownRestrictionType.NoPreviousTrack),
                            "radio" => new Either<string, SpotifyKnownRestrictionType>(SpotifyKnownRestrictionType
                                .Radio),
                            "autoplay" => new Either<string, SpotifyKnownRestrictionType>(
                                SpotifyKnownRestrictionType.AutoPlay),
                            _ => new Either<string, SpotifyKnownRestrictionType>(x)
                        };
                    }).ToImmutableArray();
                }

                if (playerState.Restrictions.DisallowPausingReasons.Count > 0)
                {
                    restrictions.Add(SpotifyRestrictionAppliesForType.Pausing,
                        MutateTo(playerState.Restrictions.DisallowPausingReasons));
                }

                if (playerState.Restrictions.DisallowResumingReasons.Count > 0)
                {
                    restrictions.Add(SpotifyRestrictionAppliesForType.Resuming,
                        MutateTo(playerState.Restrictions.DisallowResumingReasons));
                }

                if (playerState.Restrictions.DisallowSeekingReasons.Count > 0)
                {
                    restrictions.Add(SpotifyRestrictionAppliesForType.Seeking,
                        MutateTo(playerState.Restrictions.DisallowSeekingReasons));
                }

                if (playerState.Restrictions.DisallowSkippingNextReasons.Count > 0)
                {
                    restrictions.Add(SpotifyRestrictionAppliesForType.SkippingNext,
                        MutateTo(playerState.Restrictions.DisallowSkippingNextReasons));
                }

                if (playerState.Restrictions.DisallowSkippingPrevReasons.Count > 0)
                {
                    restrictions.Add(SpotifyRestrictionAppliesForType.SkippingPrevious,
                        MutateTo(playerState.Restrictions.DisallowSkippingPrevReasons));
                }

                if (playerState.Restrictions.DisallowTogglingRepeatContextReasons.Count > 0)
                {
                    restrictions.Add(SpotifyRestrictionAppliesForType.RepeatContext,
                        MutateTo(playerState.Restrictions.DisallowTogglingRepeatContextReasons));
                }

                if (playerState.Restrictions.DisallowTogglingRepeatTrackReasons.Count > 0)
                {
                    restrictions.Add(SpotifyRestrictionAppliesForType.RepeatTrack,
                        MutateTo(playerState.Restrictions.DisallowTogglingRepeatTrackReasons));
                }

                if (playerState.Restrictions.DisallowTogglingShuffleReasons.Count > 0)
                {
                    restrictions.Add(SpotifyRestrictionAppliesForType.Shuffle,
                        MutateTo(playerState.Restrictions.DisallowTogglingShuffleReasons));
                }
            }


            return new WaveeSpotifyRemoteState
            {
                Item = item,
                Context = context,
                IsPaused = isPaused,
                RepeatState = repeatState,
                IsShuffling = isShuffling,
                PositionStopwatch = isPaused
                    ? new Stopwatch()
                    : Stopwatch.StartNew(),
                PositionOffset = offset,
                Restrictions = restrictions
            };
        }
        catch (Exception x)
        {
            return new WaveeSpotifyRemoteState
            {
                Item = Option<SpotifySimpleContextItem>.None(),
                Context = Option<SpotifySimpleContext>.None(),
                IsPaused = false,
                RepeatState = WaveeRepeatStateType.None,
                IsShuffling = false,
                PositionStopwatch = new Stopwatch(),
                PositionOffset = TimeSpan.Zero,
                Restrictions =
                    new Dictionary<SpotifyRestrictionAppliesForType,
                        ImmutableArray<Either<string, SpotifyKnownRestrictionType>>>()
            };
        }
    }
}