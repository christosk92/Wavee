using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Eum.Spotify;
using Google.Protobuf;
using Wavee.Common;
using Wavee.Infrastructure.Live;
using Wavee.Spotify.Playback.Sys;
using Wavee.Spotify.Remote.State;
using Wavee.Spotify.Remote.Sys;
using Wavee.Spotify.Sys.AudioKey;
using Wavee.Spotify.Sys.Common;
using Wavee.Spotify.Sys.Connection;
using Wavee.Spotify.Sys.Metadata;
using Wavee.Spotify.Sys.Tokens;

namespace Wavee.Spotify.Remote;

public static class SpotifyRemoteClient
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="connectionInfo"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="???"></exception>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<SpotifyRemoteInfo> ConnectRemote(
        this SpotifyConnectionInfo connectionInfo,
        SpotifyPlaybackConfig config,
        CancellationToken cancellationToken = default)
    {
        var deviceId = connectionInfo.Deviceid;
        var getBearerFunc = () => connectionInfo.FetchAccessToken();
        var usernameRef = connectionInfo.WelcomeMessage;
        SpotifyRemoteInfo remoteInfo = new SpotifyRemoteInfo();


        Aff<WaveeRuntime, TrackOrEpisode> GetTrackFunc(SpotifyId id)
        {
            return GetTrackOrEpisode(connectionInfo, id);
        }

        var connectionId = await SpotifyRemote<WaveeRuntime>.Connect(remoteInfo,
            usernameRef,
            deviceId,
            getBearerFunc,
            connectionInfo.GetAudioKey<WaveeRuntime>,
            GetTrackFunc,
            config,
            (err, info) => OnDisconnected(deviceId, usernameRef,
                GetTrackFunc,
                connectionInfo.GetAudioKey<WaveeRuntime>,
                getBearerFunc, config, err, info),
            cancellationToken).Run(WaveeCore.Runtime);

        return connectionId
            .Match(
                Succ: g =>
                {
                    remoteInfo = g;
                    return g;
                },
                Fail: e => throw e
            );
    }

    private static Aff<WaveeRuntime, TrackOrEpisode> GetTrackOrEpisode(SpotifyConnectionInfo connectionInfo,
        SpotifyId id)
    {
        return id.Type switch
        {
            AudioItemType.Track => connectionInfo.GetTrack(id.ToHex(), CancellationToken.None)
                .Map(x => new TrackOrEpisode(Right(x)))
                .ToAff(),
            AudioItemType.Episode => connectionInfo.GetEpisode(id.ToHex(), CancellationToken.None)
                .Map(x => new TrackOrEpisode(Left(x)))
                .ToAff(),
            _ => FailEff<TrackOrEpisode>(new Exception("Invalid id type"))
        };
    }

    private static async void OnDisconnected(
        string deviceId,
        LanguageExt.Ref<Option<APWelcome>> userIdref,
        Func<SpotifyId, Aff<WaveeRuntime, TrackOrEpisode>> getTrackFunc,
        Func<SpotifyId, ByteString, CancellationToken, Aff<WaveeRuntime, Either<AesKeyError, ReadOnlyMemory<byte>>>>
            getAudioKeyFunc,
        Func<ValueTask<string>> getBearerFunc,
        SpotifyPlaybackConfig config,
        Option<Error> error,
        SpotifyRemoteInfo remoteInfo)
    {
        if (error.IsSome)
        {
            //probably need to reconnect
            bool connected = false;
            while (!connected)
            {
                await Task.Delay(3000);
                var newconnId = await SpotifyRemote<WaveeRuntime>.Connect(
                        remoteInfo,
                        userIdref,
                        deviceId,
                        getBearerFunc,
                        getAudioKeyFunc,
                        getTrackFunc,
                        config,
                        (err, info) => OnDisconnected(deviceId, userIdref,
                            getTrackFunc,
                            getAudioKeyFunc,
                            getBearerFunc,
                            config, err, info),
                        CancellationToken.None)
                    .Run(WaveeCore.Runtime);
                if (newconnId.IsSucc)
                {
                    var newconn = newconnId.Match(Succ: x => x, Fail: e => throw e);
                    connected = true;
                    atomic(() => remoteInfo.SpotifyConnectionId.Swap(k => newconn.ConnectionId));
                }
                else
                {
                    var err = newconnId.Match(Succ: x => throw new Exception(), Fail: e => e);
                    Debug.WriteLine($"Failed to reconnect: {err}");
                }
            }
        }
    }
}