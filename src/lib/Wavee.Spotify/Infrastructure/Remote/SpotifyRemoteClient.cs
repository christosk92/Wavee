using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Eum.Spotify.connectstate;
using Eum.Spotify.context;
using Google.Protobuf.Collections;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Core.Ids;
using Wavee.Core.Playback;
using Wavee.Core.Player;
using Wavee.Core.Player.PlaybackStates;
using Wavee.Spotify.Infrastructure.ApResolver;
using Wavee.Spotify.Infrastructure.Mercury;
using Wavee.Spotify.Infrastructure.Mercury.Token;
using Wavee.Spotify.Infrastructure.Playback;
using Wavee.Spotify.Infrastructure.Remote.Messaging;

namespace Wavee.Spotify.Infrastructure.Remote;

public class SpotifyRemoteClient
{
    private readonly SpotifyRemoteConnection _connection;
    private readonly MercuryClient _mercuryClient;
    private readonly TokenClient _tokenClient;
    private readonly SpotifyRemoteConfig _config;
    private readonly Option<uint> _lastCommandId = Option<uint>.None;
    private readonly Option<string> _lastCommandSentBy = Option<string>.None;

    private readonly string _deviceId;
    internal SpotifyRemoteClient(MercuryClient mercuryClient, TokenClient tokenClient, SpotifyRemoteConfig config,
        string deviceId,
        string userId)
    {
        _mercuryClient = mercuryClient;
        _tokenClient = tokenClient;
        _config = config;
        _deviceId = deviceId;
        _connection = new SpotifyRemoteConnection(userId);

        var mn = new ManualResetEvent(false);
        Task.Run(async () =>
        {
            await SpotifyRemoteRuntime.Start(_connection, _mercuryClient, _tokenClient, _config, deviceId,
                onLost: (e) => { mn.Reset(); });
            mn.Set();
        });

        SpotifyLocalDeviceState localState = SpotifyLocalDeviceState.New(
            deviceId: deviceId,
            deviceName: config.DeviceName,
            deviceType: config.DeviceType
        );

        bool wasActive = false;
        //setup a player listener
        WaveePlayer.StateChanged
            .Select(cm =>
            {
                //wait for connection
                mn.WaitOne();
                return cm;
            })
            .Select(c => MutateFromPlayer(c, localState, wasActive))
            .Select(async localDeviceState =>
            {
                localState = localDeviceState;
                wasActive = localDeviceState.IsActive;
                //send the state to spotify
                var putState = localDeviceState.BuildPutState(
                    PutStateReason.PlayerStateChanged,
                    playerTime: WaveePlayer.Position
                );

                var spClient = await ApResolve.GetSpClient(CancellationToken.None);
                var spClientUrl = $"https://{spClient.host}:{spClient.port}";
                var cluster = await SpotifyRemoteRuntime.PutState(
                    deviceId, spClientUrl,
                    putState,
                    _connection.ConnectionId.ValueUnsafe(),
                    _tokenClient,
                    CancellationToken.None);
            })
            .Subscribe();
    }

    private SpotifyLocalDeviceState MutateFromPlayer(
        WaveePlayerState state,
        SpotifyLocalDeviceState spotifyLocalDeviceState,
        bool wasActive)
    {
        bool isActiveNow = state.PlaybackState is { IsPlaying: true, TrackId.Service: ServiceType.Spotify };
        if (!wasActive && isActiveNow)
        {
            spotifyLocalDeviceState = spotifyLocalDeviceState with
            {
                IsActive = true,
                PlayingSince = DateTimeOffset.UtcNow
            };

            spotifyLocalDeviceState = spotifyLocalDeviceState with
            {
                LastCommandId = _lastCommandId,
                LastCommandSentBy = _lastCommandSentBy
            };
        }
        else if (!isActiveNow && wasActive)
        {
            return SpotifyLocalDeviceState.New(
                deviceId: spotifyLocalDeviceState.DeviceId,
                deviceName: spotifyLocalDeviceState.DeviceName,
                deviceType: spotifyLocalDeviceState.DeviceType
            );
        }

        return spotifyLocalDeviceState.SetStateFrom(state);
    }

    public IObservable<SpotifyLibraryUpdateNotification> LibraryChanged =>
        _connection.OnLibraryNotification;
    public IObservable<SpotifyRootlistUpdateNotification> RootlistChanged =>
        _connection.OnRootListNotification;
    public IObservable<SpotifyRemoteState> StateChanged =>
        _connection
            .OnClusterChange()
            .Throttle(TimeSpan.FromMilliseconds(50))
            .Select(c => SpotifyRemoteState.From(_deviceId, c));


    public async ValueTask<Unit> SkipNext(CancellationToken ct)
    {
        var toDeviceId = _connection._latestCluster.Value.Map(x => x.ActiveDeviceId);
        if (toDeviceId.IsNone)
        {
            return default;
        }
        var command = new
        {
            command = new
            {
                endpoint = "skip_next",
            }
        };
        var sp = await SpClientUrl(ct);
        await SpotifyRemoteRuntime.InvokeCommandOnRemoteDevice(
            command,
            sp,
            toDeviceId.ValueUnsafe(),
            _deviceId,
            _tokenClient,
            ct);
        return default;
    }

    public async ValueTask<Unit> SkipPrevious(CancellationToken ct)
    {
        var toDeviceId = _connection._latestCluster.Value.Map(x => x.ActiveDeviceId);
        if (toDeviceId.IsNone)
        {
            return default;
        }
        var command = new
        {
            command = new
            {
                endpoint = "skip_prev",
            }
        };
        var sp = await SpClientUrl(ct);
        await SpotifyRemoteRuntime.InvokeCommandOnRemoteDevice(
            command,
            sp,
            toDeviceId.ValueUnsafe(),
            _deviceId,
            _tokenClient,
            ct);
        return default;
    }

    public async ValueTask<Unit> SetRepeatState(RepeatState repeatState, CancellationToken ct)
    {
        var toDeviceId = _connection._latestCluster.Value.Map(x => x.ActiveDeviceId);
        if (toDeviceId.IsNone)
        {
            return default;
        }
        var command = new
        {
            command = new
            {
                endpoint = "set_options",
                repeating_context = repeatState >= RepeatState.Context,
                repeating_track = repeatState is RepeatState.Track
            }
        };
        var sp = await SpClientUrl(ct);
        await SpotifyRemoteRuntime.InvokeCommandOnRemoteDevice(
                       command,
                                  sp,
                                  toDeviceId.ValueUnsafe(),
                                  _deviceId,
                                  _tokenClient,
                                  ct);
        return default;
    }

    public async ValueTask<Unit> SetShuffleState(bool shuffling, CancellationToken ct)
    {
        var toDeviceId = _connection._latestCluster.Value.Map(x => x.ActiveDeviceId);
        if (toDeviceId.IsNone)
        {
            return default;
        }
        var command = new
        {
            command = new
            {
                endpoint = "set_shuffling_context",
                value = shuffling
            }
        };
        var sp = await SpClientUrl(ct);
        await SpotifyRemoteRuntime.InvokeCommandOnRemoteDevice(
            command,
            sp,
            toDeviceId.ValueUnsafe(),
            _deviceId,
            _tokenClient,
            ct);
        return default;
    }
    public async ValueTask<Unit> Resume(CancellationToken ct = default)
    {
        var toDeviceId = _connection._latestCluster.Value.Map(x => x.ActiveDeviceId);
        if (toDeviceId.IsNone)
        {
            return default;
        }
        // https://gae2-spclient.spotify.com/connect-state/v1/player/command/from/1b5327f43e39a20de0ec1dcafa3466f082e28348/to/342d539fa2bc06a1cfa4d03d67c3d90513b75879
        var command = new
        {
            command = new
            {
                endpoint = "resume"
            }
        };
        var sp = await SpClientUrl(ct);
        await SpotifyRemoteRuntime.InvokeCommandOnRemoteDevice(
            command,
            sp,
            toDeviceId.ValueUnsafe(),
            _deviceId,
            _tokenClient,
            ct);
        return default;
    }

    public async ValueTask<Unit> PlayContextRaw(AudioId contextId, string contextUrl, int trackIndex, Option<AudioId> trackId)
    {
        var toDeviceId = _connection._latestCluster.Value.Map(x => x.ActiveDeviceId);
        if (toDeviceId.IsNone)
        {
            return default;
        }
        // https://gae2-spclient.spotify.com/connect-state/v1/player/command/from/1b5327f43e39a20de0ec1dcafa3466f082e28348/to/342d539fa2bc06a1cfa4d03d67c3d90513b75879
        /*
         * {
     "command": {
         "context": {
             "uri": "spotify:artist:1qma7XhwZotCAucL7NHVLY",
             "url": "...",
         },
         "options": {
             "license": "premium",
             "skip_to": {
                 "track_index": 4,
                 "page_index": 0
             },
             "player_options_override": {}
         },
         "endpoint": "play"
     }
 }
         */

        object command;
        if (trackId.IsSome)
        {
            command = new
            {
                command = new
                {
                    context = new
                    {
                        uri = contextId.ToString(),
                        url = contextUrl
                    },
                    options = new
                    {
                        license = "premium",
                        skip_to = new
                        {
                            track_index = trackIndex,
                            track_uri = trackId.ValueUnsafe().ToString()
                        },
                        player_options_override = new object()
                    },
                    endpoint = "play"
                }
            };
        }
        else
        {
            command = new
            {
                command = new
                {
                    context = new
                    {
                        uri = contextId.ToString(),
                        url = contextUrl
                    },
                    options = new
                    {
                        license = "premium",
                        skip_to = new
                        {
                            track_index = trackIndex
                        },
                        player_options_override = new object()
                    },
                    endpoint = "play"
                }
            };
        }

        var sp = await SpClientUrl(CancellationToken.None);
        await SpotifyRemoteRuntime.InvokeCommandOnRemoteDevice(
            command,
            sp,
            toDeviceId.ValueUnsafe(),
            _deviceId,
            _tokenClient,
            CancellationToken.None);
        return default;
    }
    public async ValueTask<Unit> PlayContextPaged(
        AudioId contextId,
        IEnumerable<ContextPage> pages,
        int trackIndex,
        int pageIndex)
    {
        var toDeviceId = _connection._latestCluster.Value.Map(x => x.ActiveDeviceId);
        if (toDeviceId.IsNone)
        {
            return default;
        }
        // https://gae2-spclient.spotify.com/connect-state/v1/player/command/from/1b5327f43e39a20de0ec1dcafa3466f082e28348/to/342d539fa2bc06a1cfa4d03d67c3d90513b75879
        /*
         * {
     "command": {
         "context": {
             "uri": "spotify:artist:1qma7XhwZotCAucL7NHVLY",
             "metadata": {},
             "pages": [..]
         },
         "options": {
             "license": "premium",
             "skip_to": {
                 "track_index": 4,
                 "page_index": 0
             },
             "player_options_override": {}
         },
         "endpoint": "play"
     }
 }
         */

        var command = new
        {
            command = new
            {
                context = new
                {
                    uri = contextId.ToString(),
                    pages = pages.Select(c => new
                    {
                        page_url = c.PageUrl
                    }),
                },
                options = new
                {
                    license = "premium",
                    skip_to = new
                    {
                        track_index = trackIndex,
                        page_index = pageIndex
                    },
                    player_options_override = new object()
                },
                endpoint = "play"
            }
        };

        var sp = await SpClientUrl(CancellationToken.None);
        await SpotifyRemoteRuntime.InvokeCommandOnRemoteDevice(
            command,
            sp,
            toDeviceId.ValueUnsafe(),
            _deviceId,
            _tokenClient,
            CancellationToken.None);
        return default;
    }
    public async ValueTask<Unit> Pause(CancellationToken ct = default)
    {
        var toDeviceId = _connection._latestCluster.Value.Map(x => x.ActiveDeviceId);
        if (toDeviceId.IsNone)
        {
            return default;
        }
        // https://gae2-spclient.spotify.com/connect-state/v1/player/command/from/1b5327f43e39a20de0ec1dcafa3466f082e28348/to/342d539fa2bc06a1cfa4d03d67c3d90513b75879
        var command = new
        {
            command = new
            {
                endpoint = "pause"
            }
        };
        var sp = await SpClientUrl(ct);
        await SpotifyRemoteRuntime.InvokeCommandOnRemoteDevice(
            command,
            sp,
            toDeviceId.ValueUnsafe(),
            _deviceId,
            _tokenClient,
            ct);
        return default;
    }
    public async ValueTask<Unit> SeekTo(double to, CancellationToken ct = default)
    {
        var toDeviceId = _connection._latestCluster.Value.Map(x => x.ActiveDeviceId);
        if (toDeviceId.IsNone)
        {
            return default;
        }
        // https://gae2-spclient.spotify.com/connect-state/v1/player/command/from/1b5327f43e39a20de0ec1dcafa3466f082e28348/to/342d539fa2bc06a1cfa4d03d67c3d90513b75879
        var command = new
        {
            command = new
            {
                endpoint = "seek_to",
                value = to
            }
        };
        var sp = await SpClientUrl(ct);
        await SpotifyRemoteRuntime.InvokeCommandOnRemoteDevice(
            command,
            sp,
            toDeviceId.ValueUnsafe(),
            _deviceId,
            _tokenClient,
            ct);
        return default;
    }
    public async ValueTask<Unit> SetVolume(double newVolumeFrac, CancellationToken ct)
    {
        var toDeviceId = _connection._latestCluster.Value.Map(x => x.ActiveDeviceId);
        if (toDeviceId.IsNone)
        {
            return default;
        }
        //SetVolume
        var volumeAsInteger = Math.Clamp((int)(newVolumeFrac * ushort.MaxValue), 0, ushort.MaxValue);
        var command = new
        {
            volume = volumeAsInteger
        };
        var sp = await SpClientUrl(ct);
        await SpotifyRemoteRuntime.SetVolume(
                       command,
                       sp,
                       _deviceId,
                       toDeviceId.ValueUnsafe(),
                       _tokenClient,
                       ct);
        return unit;
    }
    private static string _spClientUrl;
    private static async ValueTask<string> SpClientUrl(CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_spClientUrl))
            return _spClientUrl;
        var spClient = await ApResolve.GetSpClient(ct);
        var spClientUrl = $"https://{spClient.host}:{spClient.port}";
        _spClientUrl = spClientUrl;
        return spClientUrl;
    }
}