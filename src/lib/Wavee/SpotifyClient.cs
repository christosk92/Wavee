using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using Eum.Spotify;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Serilog;
using Wavee.AudioKey;
using Wavee.AudioKey.Live;
using Wavee.Cache;
using Wavee.Cache.Live;
using Wavee.ContextResolve;
using Wavee.ContextResolve.Live;
using Wavee.GraphQL;
using Wavee.Infrastructure.Connection;
using Wavee.Infrastructure.Playback;
using Wavee.Infrastructure.Remote;
using Wavee.Metadata;
using Wavee.Metadata.Artist;
using Wavee.Metadata.Live;
using Wavee.Playback;
using Wavee.Playback.Live;
using Wavee.Player;
using Wavee.Remote;
using Wavee.Remote.Live;
using Wavee.Spotify.Infrastructure.Connection;
using Wavee.Token;
using Wavee.Token.Live;

namespace Wavee;

public class SpotifyClient
{
    private Guid _connectionId;
    private TaskCompletionSource<string> _countryCodeTask;
    private readonly TaskCompletionSource<Unit> _waitForConnectionTask;
    private readonly IWaveePlayer _player;

    internal static Dictionary<Guid, SpotifyClient> Clients = new();
    private readonly SpotifyConfig _config;

    public SpotifyClient(IWaveePlayer player, LoginCredentials credentials, SpotifyConfig config)
    {
        _player = player;
        _config = config;
        Cache = new LiveSpotifyCache(
            Config: config.Cache
        );
        var deviceId = Guid.NewGuid().ToString("N");
        _waitForConnectionTask = new TaskCompletionSource<Unit>(TaskCreationOptions.RunContinuationsAsynchronously);

        void CreateConnectionRecursively()
        {
            _countryCodeTask = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            _connectionId = ConnectionFactory(credentials, config, deviceId, async (err) =>
            {
                Log.Logger.Error(err, "Connection lost");
                await Task.Delay(2000);
                CreateConnectionRecursively();
            });
            Clients[_connectionId] = this;

            //setup country code
            var listener = _connectionId.CreateListener((ref SpotifyUnencryptedPackage package) =>
                package.Type is SpotifyPacketType.CountryCode);
            Task.Run(async () =>
            {
                try
                {
                    if (await listener.Reader.WaitToReadAsync())
                    {
                        var countryCode = await listener.Reader.ReadAsync();
                        var countryCodeString = countryCode.Payload.Span.GetUTF8String();
                        _countryCodeTask.TrySetResult(countryCodeString);
                    }
                }
                catch (Exception e)
                {
                    _countryCodeTask.SetException(e);
                }
                finally
                {
                    listener.Finished();
                }
            });
        }

        CreateConnectionRecursively();
        _ = Task.Run(async () =>
        {
            await SpotifyRemoteConnection.Create(
                deviceId: deviceId,
                connectionId: _connectionId,
                accessToken: Token.GetToken,
                config: config
            );
            _waitForConnectionTask.TrySetResult(Unit.Default);

            //Setup remote command listener
            var remoteCommandListener = _connectionId.CreateListener(x =>
                x.Type is SpotifyRemoteMessageType.Request || (x.Uri.StartsWith("hm://connect-state/v1/volume")));

            await Task.Factory.StartNew(async () =>
            {
                try
                {
                    await foreach (var package in remoteCommandListener.Reader.ReadAllAsync())
                    {
                        if (package.Type is SpotifyRemoteMessageType.Request)
                        {
                            using var jsondoc = JsonDocument.Parse(package.Payload);
                            //  "{\"message_id\":1598341393,\"target_alias_id\":null,\"sent_by_device_id\":\"eae17cd4d59caffc37a401f0789e329ff154b1e5\",\"command\":{\"endpoint\":\"transfer\",\"data\":\"CgYIABABGAAS5AcIp4aK+o0xEPALGQAAAAAAAPA/IAAqzAcKABJQNjY2MTM0MzYzODM5NjMzODYxMzI2MTYyMzAzOTYxNjEzMDY2NjIzMDM5NjU2NjYxNjUzMDYxNjEzMjM5MzA2NjMxMzQzNTM5MzM2MTM1NjYaECu4D8ZsbU1EkyY75/XBYLAiMwoLZGVjaXNpb25faWQSJDA1ZmVhN2Y4ZDNkMjVhMWQ2MzZmZmEyZTMyOWQxNTEyNjg5OCJKChBpbWFnZV94bGFyZ2VfdXJsEjZzcG90aWZ5OmltYWdlOmFiNjc2MTZkMDAwMGIyNzNkMTJjNTc2ZmQwOTNiMDlmNjJhMGM1NjQiOAoQcGFnZV9pbnN0YW5jZV9pZBIkZTYwMzc3ODctZjEzOC00NjNkLWIzMDAtYzJjYTJkYmQzNTgzIg4KCWl0ZXJhdGlvbhIBMCIxCglhbGJ1bV91cmkSJHNwb3RpZnk6YWxidW06NldUVjVXY2tUUUkyRmp5STVZUDFQRyI2Cgtjb250ZXh0X3VyaRInc3BvdGlmeTpwbGF5bGlzdDozN2k5ZFFaRjFFOE5WRkhSb2xHNmpIIkAKC2FsYnVtX3RpdGxlEjFOZXZlcnRoZWxlc3MsIChPcmlnaW5hbCBEcmFtYSBTb3VuZCBUcmFjaywgUHQuIDEpIjYKDmludGVyYWN0aW9uX2lkEiQzZTMzMDMyZi0wYmVlLTRlYTgtODBkMy0zM2ZkYjRmOWM1ZDkiMwoKYXJ0aXN0X3VyaRIlc3BvdGlmeTphcnRpc3Q6MU5WUnZWMEtxYU83VnRTYVZRY20zViJJCg9pbWFnZV9sYXJnZV91cmwSNnNwb3RpZnk6aW1hZ2U6YWI2NzYxNmQwMDAwYjI3M2QxMmM1NzZmZDA5M2IwOWY2MmEwYzU2NCJJCg9pbWFnZV9zbWFsbF91cmwSNnNwb3RpZnk6aW1hZ2U6YWI2NzYxNmQwMDAwNDg1MWQxMmM1NzZmZDA5M2IwOWY2MmEwYzU2NCIVCgx0cmFja19wbGF5ZXISBWF1ZGlvIjUKCmVudGl0eV91cmkSJ3Nwb3RpZnk6cGxheWxpc3Q6MzdpOWRRWkYxRThOVkZIUm9sRzZqSCIqCiBhY3Rpb25zLnNraXBwaW5nX3ByZXZfcGFzdF90cmFjaxIGcmVzdW1lIioKIGFjdGlvbnMuc2tpcHBpbmdfbmV4dF9wYXN0X3RyYWNrEgZyZXN1bWUiQwoJaW1hZ2VfdXJsEjZzcG90aWZ5OmltYWdlOmFiNjc2MTZkMDAwMDFlMDJkMTJjNTc2ZmQwOTNiMDlmNjJhMGM1NjQazwYKSgoIcGxheWxpc3QSJXhwdWlfMjAyMy0wNi0wNV8xNjg1OTc3NzUyMTMyX2E1ODhmNzQaACIAKg9ub3dfcGxheWluZ19iYXIyAEIAEtwECidzcG90aWZ5OnBsYXlsaXN0OjM3aTlkUVpGMUU4TlZGSFJvbEc2akgSMWNvbnRleHQ6Ly9zcG90aWZ5OnBsYXlsaXN0OjM3aTlkUVpGMUU4TlZGSFJvbEc2akgaDQoJaW1hZ2VfdXJsEgAaIwoTY29udGV4dF9kZXNjcmlwdGlvbhIM7Iuc7J6RIFJhZGlvGiIKEGZvcm1hdF9saXN0X3R5cGUSDmluc3BpcmVkYnktbWl4GjwKEXplbGRhLmNvbnRleHRfdXJpEidzcG90aWZ5OnBsYXlsaXN0OjM3aTlkUVpGMUU4TlZGSFJvbEc2akgaMgoKcmVxdWVzdF9pZBIkMDVmZWE3ZjhkM2QyNWExZDYzNmZmYTJlMzI5ZDE1MTI2ODk4GmwKD21lZGlhTGlzdENvbmZpZxJZc3BvdGlmeTptZWRpYWxpc3Rjb25maWc6dHJhY2stcmFkaW8taW5zcGlyZWRieTp0ZXN0OnEzXzIwMjNfZ3BzX2JhcmJpZV8yX3JhZGlvOmRlZmF1bHRfdjMaNgoOY29ycmVsYXRpb24taWQSJDA1ZmVhN2Y4ZDNkMjVhMWQ2MzZmZmEyZTMyOWQxNTEyNjg5OBofChlwbGF5bGlzdF9udW1iZXJfb2ZfdHJhY2tzEgI1MBoYCg1jb250ZXh0X293bmVyEgdzcG90aWZ5Gi0KEG1hZGVGb3IudXNlcm5hbWUSGTd1Y2doZGdxdWY2YnlxdXNxa2xpbHR3YzIaIAobcGxheWxpc3RfbnVtYmVyX29mX2VwaXNvZGVzEgEwIgAwARpQNjY2MTM0MzYzODM5NjMzODYxMzI2MTYyMzAzOTYxNjEzMDY2NjIzMDM5NjU2NjYxNjUzMDYxNjEzMjM5MzA2NjMxMzQzNTM5MzM2MTM1NjYiACoAMkwyJDNlMzMwMzJmLTBiZWUtNGVhOC04MGQzLTMzZmRiNGY5YzVkOTokZTYwMzc3ODctZjEzOC00NjNkLWIzMDAtYzJjYTJkYmQzNTgzIgIQAA==\",\"options\":{\"restore_paused\":\"restore\",\"restore_position\":\"extrapolate\",\"restore_track\":\"always_play_something\",\"license\":\"premium\"},\"from_device_identifier\":\"eae17cd4d59caffc37a401f0789e329ff154b1e5\"}}"
                            var messageId = jsondoc.RootElement.GetProperty("message_id").GetUInt32();
                            var sentByDeviceId = jsondoc.RootElement.GetProperty("sent_by_device_id").GetString();
                            var command = jsondoc.RootElement.GetProperty("command");
                            var endpoint = command.GetProperty("endpoint").GetString();

                            var remoteCommand = new SpotifyCommand(
                                MessageId: messageId,
                                SentByDeviceId: sentByDeviceId,
                                Endpoint: endpoint,
                                Data: command.TryGetProperty("data", out var dt)
                                    ? dt.GetBytesFromBase64()
                                    : ReadOnlyMemory<byte>.Empty
                            );

                            if (Playback is LiveSpotifyPlaybackClient playback)
                                await playback.RemoteCommand(remoteCommand);
                        }
                        else if (package.Uri.StartsWith("hm://connect-state/v1/volume"))
                        {
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Logger.Error(e, "Error in remote command listener");
                }
                finally
                {
                    remoteCommandListener.onDone();
                }
            });
        });


        //Setup player listener
        var wasActive = false;

        SpotifyLocalPlaybackState prevState = SpotifyLocalPlaybackState.Empty(
            config: config.Remote,
            deviceId: deviceId
        );
        var playerListener = SpotifyPlaybackHandler.Setup(connectionId: _connectionId, player)
            .Select(x =>
            {
                var isNowActive = x.IsSome;
                var activeChanged = isNowActive != wasActive;
                if (!wasActive && !isNowActive)
                {
                    //do nothing
                    return Option<SpotifyLocalPlaybackState>.None;
                }

                return prevState.FromPlayer(x, isNowActive, activeChanged, Option<string>.None, Option<uint>.None);
            })
            .SelectMany(async state =>
            {
                if (state.IsSome)
                {
                    prevState = state.ValueUnsafe();
                    await PlaybackStateChanged(state.ValueUnsafe());
                }

                return Unit.Default;
            })
            .Subscribe();
    }

    public ITokenClient Token => new LiveTokenClient(connId: _connectionId);
    public IMercuryClient Mercury => new LiveTokenClient(connId: _connectionId);

    public ISpotifyRemoteClient Remote =>
        new LiveSpotifyRemoteClient(_connectionId, waitForConnectionTask: _waitForConnectionTask);

    public IContextResolver ContextResolver => new LiveContextResolver(() => Mercury);

    public ISpotifyPlaybackClient Playback => new LiveSpotifyPlaybackClient(_connectionId,
        remoteClient: new WeakReference<ISpotifyRemoteClient>(Remote), waitForConnectionTask: _waitForConnectionTask);

    public ISpotifyMetadataClient Metadata => new LiveSpotifyMetadataClient(mercuryFactory: () => Mercury,
        _countryCodeTask.Task, _graphQLQuery, Cache);

    private Task<HttpResponseMessage> _graphQLQuery(IGraphQLQuery arg) =>
        new LiveGraphQLClient(fetchAccessTokenFactory: (CancellationToken ct) => Token.GetToken(ct)).Query(arg);

    public ISpotifyAudioKeysClient AudioKeys => new LiveSpotifyAudioKeysClient(_connectionId);

    public ISpotifyCache Cache { get; }


    public ValueTask<string> Country => _countryCodeTask.Task.IsCompleted
        ? new ValueTask<string>(_countryCodeTask.Task.Result)
        : new ValueTask<string>(_countryCodeTask.Task);

    public SpotifyConfig Config => _config;


    private async Task PlaybackStateChanged(SpotifyLocalPlaybackState obj)
    {
        await _waitForConnectionTask.Task;

        if (Remote is LiveSpotifyRemoteClient remote)
            await remote.PlaybackStateUpdated(obj);
    }

    private static Guid ConnectionFactory(LoginCredentials credentials, SpotifyConfig config,
        string deviceId,
        Action<Exception> onConnectionLost)
    {
        Guid? connectionId = null;
        connectionId = SpotifyConnection.Create(credentials, config, deviceId, onConnectionLost, connectionId);
        return connectionId.Value;
    }
}