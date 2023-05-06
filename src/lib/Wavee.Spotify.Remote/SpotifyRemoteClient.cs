using Eum.Spotify.connectstate;
using Eum.Spotify.context;
using Eum.Spotify.transfer;
using LanguageExt;
using Wavee.Common;
using Wavee.Infrastructure.Traits;
using Wavee.Player;
using Wavee.Player.Context;
using Wavee.Player.Playback;
using Wavee.Spotify.Contracts;
using Wavee.Spotify.Contracts.Common;
using Wavee.Spotify.Contracts.Remote;
using Wavee.Spotify.Playback;
using Wavee.Spotify.Playback.Infrastructure.Sys;
using Wavee.Spotify.Remote.Infrastructure.State;
using Wavee.Spotify.Remote.Infrastructure.State.Messages;
using Wavee.Spotify.Remote.Infrastructure.Sys;
using Queue = Eum.Spotify.transfer.Queue;

namespace Wavee.Spotify.Remote;

internal sealed class SpotifyRemoteClient<RT> : ISpotifyRemoteClient where RT : struct, HasWebsocket<RT>, HasHttp<RT>
{
    private readonly Ref<SpotifyRemoteState> _remoteState;
    private readonly Ref<Option<Cluster>> _cluster;
    private readonly Func<Task<string>> _getBearer;

    private readonly RT _runtime;
    private readonly string _spClientUrl;

    public SpotifyRemoteClient(string spClientUrl, SpotifyRemoteState newRemoteState,
        Cluster cluster,
        Func<Task<string>> getBearer, RT runtime)
    {
        _remoteState = Ref(newRemoteState);
        _cluster = Ref(Some(cluster));
        _spClientUrl = spClientUrl;
        _getBearer = getBearer;
        _runtime = runtime;
    }

    public Option<Cluster> Cluster => _cluster.Value;
    public IObservable<Option<Cluster>> ClusterUpdated => _cluster.OnChange();

    internal Unit OnCluster(ClusterUpdate parseFrom)
    {
        atomic(() => _cluster.Swap(_ => Some(parseFrom.Cluster)));
        atomic(() => _remoteState.Swap(s => s.FromCluster(parseFrom.Cluster)));
        return unit;
    }

    public Aff<RT, bool> OnRequest(
        ISpotifyClient client,
        IWaveePlayer player,
        string key, SpotifyRequestMessage spotifyRequestCommand)
    {
        atomic(() => _remoteState.Swap(r => r.NewCommand(spotifyRequestCommand)));
        switch (spotifyRequestCommand.CommandEndpoint)
        {
            case RequestCommandEndpointType.Transfer:
                return TransferAff(client, player, spotifyRequestCommand);
            default:
                return SuccessAff(false);
        }
    }

    private Aff<RT, bool> TransferAff(
        ISpotifyClient client,
        IWaveePlayer player,
        SpotifyRequestMessage spotifyRequestCommand) =>
        from tranferState in Eff(() => TransferState.Parser.ParseFrom(spotifyRequestCommand.CommandPayload.Span))
        from ctx in BuildPlayContext(client, tranferState)
        from startFrom in GetStartPosition(tranferState)
        from play in ShouldPlay(tranferState)
        from __ in player.Command(new PlayContextCommand(ctx, Option<int>.None, startFrom, play)).ToAff()
        from updated in SpotifyRemoteRuntime<RT>.PutState(_spClientUrl,
            PutStateReason.PlayerStateChanged,
            false,
            _remoteState.Value, _getBearer)
        from _ in Eff(() => atomic(() => _remoteState.Swap(s => s.FromCluster(updated))))
        select false;

    private static Eff<RT, bool> ShouldPlay(TransferState transferState)
    {
        //TODO
        return SuccessEff(true);
    }

    private static Eff<RT, TimeSpan> GetStartPosition(TransferState tranferState)
    {
        //TODO
        return SuccessEff(TimeSpan.Zero);
    }

    private static Eff<RT, IPlayContext> BuildPlayContext(
        ISpotifyClient client,
        TransferState tranferState)
    {
        return SuccessEff((IPlayContext)new InitialLazySpotifyContext(
            tranferState.Playback.CurrentTrack,
            tranferState.Queue,
            tranferState.CurrentSession.Context,
            client
        ));
    }
}

internal sealed class InitialLazySpotifyContext : IPlayContext
{
    private readonly ContextTrack _playbackCurrentTrack;
    private readonly Queue _tranferStateQueue;
    private readonly Context _currentSessionContext;

    private readonly WeakReference<ISpotifyClient> _spotifyClient;

    public InitialLazySpotifyContext(ContextTrack playbackCurrentTrack,
        Queue tranferStateQueue,
        Context currentSessionContext, ISpotifyClient client)
    {
        _playbackCurrentTrack = playbackCurrentTrack;
        _tranferStateQueue = tranferStateQueue;
        _currentSessionContext = currentSessionContext;
        _spotifyClient = new WeakReference<ISpotifyClient>(client);
    }

    public ValueTask<(IPlaybackStream Stream, int AbsoluteIndex)> GetStreamAt(
        Either<Shuffle, Option<int>> at)
    {
        return at
            .Match(
                Left: _ => throw new NotImplementedException(),
                Right: maybeOption => maybeOption
                    .Match(
                        Some: index => throw new NotImplementedException(),
                        None: () =>
                            new ValueTask<(IPlaybackStream Stream, int AbsoluteIndex)>(
                                LoadStreamFromTrack(_playbackCurrentTrack))
                    )
            );
    }

    private async Task<(IPlaybackStream Stream, int AbsoluteIndex)> LoadStreamFromTrack(ContextTrack track)
    {
        if (!_spotifyClient.TryGetTarget(out var client))
            throw new InvalidOperationException("Spotify client is not available");
        var trackId = SpotifyId.FromGid(track.Gid, AudioItemType.Track);
        var stream = await client.StreamAudio(trackId,
            new SpotifyPlaybackConfig(PreferredQualityType.High, ushort.MaxValue / 2));
        return (stream, 0);
    }

    public ValueTask<Option<int>> Count()
    {
        throw new NotImplementedException();
    }
}