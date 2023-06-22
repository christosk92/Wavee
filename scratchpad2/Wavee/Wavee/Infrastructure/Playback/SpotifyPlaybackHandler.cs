using System.Threading.Channels;
using Google.Protobuf.WellKnownTypes;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Serilog;
using Wavee.ContextResolve;
using Wavee.Id;
using Wavee.Playback.Command;
using Wavee.Player;
using Wavee.Player.Ctx;
using Wavee.Player.State;

namespace Wavee.Infrastructure.Playback;

internal static class SpotifyPlaybackHandler
{
    private record WaveePlayerHolder(IWaveePlayer Player, ChannelWriter<ISpotifyPlaybackCommand> Writer);

    private static Dictionary<Guid, WaveePlayerHolder> _players = new();

    public static async Task Send(Guid connectionId, ISpotifyPlaybackCommand playbackEvent)
    {
        if (_players.TryGetValue(connectionId, out var playerHolder))
        {
            await playerHolder.Writer.WriteAsync(playbackEvent);
        }
    }

    public static IObservable<Option<WaveePlayerState>> Setup(Guid connectionId, IWaveePlayer player)
    {
        var channel = Channel.CreateUnbounded<ISpotifyPlaybackCommand>();
        _players.Add(connectionId, new WaveePlayerHolder(
            Player: player,
            Writer: channel.Writer
        ));

        Task.Factory.StartNew(async () =>
        {
            try
            {
                await foreach (var command in channel.Reader.ReadAllAsync())
                {
                    await OnPlaybackEvent(connectionId, command, player);
                }
            }
            finally
            {
                channel.Writer.Complete();
            }
        });

        return player.CreateListener();
    }

    private static async Task OnPlaybackEvent(Guid connectionId, ISpotifyPlaybackCommand command, IWaveePlayer player)
    {
        try
        {
            switch (command)
            {
                case SpotifyPlayCommand play:
                    await HandlePlay(connectionId, play, player);
                    break;
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Error handling playback event");
        }
    }

    private static async Task HandlePlay(Guid connectionId, SpotifyPlayCommand play, IWaveePlayer player)
    {
        var currentState = player.CurrentState;

        Option<WaveeContext> ctx;

        if (currentState.IsSome && currentState.ValueUnsafe().Context.IsSome &&
            currentState.ValueUnsafe().Context.ValueUnsafe().Id == play.ContextUri)
        {
            ctx = currentState.ValueUnsafe().Context.ValueUnsafe();
        }
        else if (play.ContextUri.IsSome)
        {
            ctx = await BuildWaveeContext(connectionId, play.ContextUri);
        }
        else
        {
            //check queue and build context from there
            ctx = Option<WaveeContext>.None;
        }

        static int findIndex(string ctxUri, IEnumerable<FutureWaveeTrack> tracks, Option<int> index, Option<string> uid,
            Option<SpotifyId> id)
        {
            if (index.IsSome)
                return index.ValueUnsafe();

            //first with uid
            int i = 0;
            foreach (var track in tracks)
            {
                if (uid.IsSome)
                {
                    if (track.TrackUid == uid.ValueUnsafe())
                        return i;
                }
                else if (id.IsSome)
                {
                    if (track.TrackId == id.ValueUnsafe().ToString())
                        return i;
                }

                i++;
            }

            //not found
            if (uid.IsSome && id.IsSome)
                return findIndex(ctxUri, tracks, Option<int>.None, Option<string>.None, id);

            if (uid.IsNone && id.IsSome)
            {
                Log.Warning("Track with id {id} not found in context {context}. Starting from 0", id.ValueUnsafe(),
                    ctxUri);
                return 0;
            }

            return 0;
        }

        int idx = 0;
        if (ctx.IsSome)
        {
            idx = findIndex(play.ContextUri.ValueUnsafe(), ctx.ValueUnsafe().FutureTracks, play.IndexInContext,
                play.TrackUid, play.TrackId);
        }
        else
        {
            ctx = new WaveeContext(
                Id: "empty",
                Name: "empty",
                FutureTracks: Enumerable.Empty<FutureWaveeTrack>(),
                ShuffleProvider: IShuffleProvider.Default
            );
        }

        await player.Play(ctx.ValueUnsafe(), idx,
            startFrom: Option<TimeSpan>.None,
            startPaused: false,
            shuffling: Option<bool>.None,
            repeatState: Option<RepeatState>.None);
    }

    private static Task<WaveeTrack> StreamTrack(SpotifyId id, HashMap<string, string> trackMetadata, string country,
        CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    private static async Task<Option<WaveeContext>> BuildWaveeContext(Guid connectionId, Option<string> playContextUri)
    {
        var client = SpotifyClient.Clients[connectionId];
        var val = playContextUri.ValueUnsafe();
        var context = await client.ContextResolver.Resolve(val, CancellationToken.None);
        var countryCode = await client.Country;
        var futureTracks = BuildFutureTracks(connectionId, context);

        return new WaveeContext(
            Id: playContextUri.ValueUnsafe(),
            Name: context.Metadata.Find("context_description").IfNone(val),
            FutureTracks: futureTracks,
            ShuffleProvider: IShuffleProvider.Default
        );
    }

    private static IEnumerable<FutureWaveeTrack> BuildFutureTracks(
        Guid connectionId,
        SpotifyContext spotifyContext)
    {
        foreach (var page in spotifyContext.Pages)
        {
            //check if the page has tracks
            //if it does, yield return each track
            //if it doesn't, fetch the next page (if next page is set). if not go to the next page
            if (page.Tracks.Count > 0)
            {
                foreach (var track in page.Tracks)
                {
                    var id = SpotifyId.FromUri(track.Uri);
                    var uid = track.HasUid ? track.Uid : Option<string>.None;
                    var trackMetadata = track.Metadata.ToHashMap();
                    var client = SpotifyClient.Clients[connectionId];
                    var country = client.Country.Result;
                    yield return new FutureWaveeTrack(id.ToString(),
                        TrackUid: uid.IfNone(id.ToBase16()),
                        (ct) => StreamTrack(id, trackMetadata, country, ct));
                }
            }
            else
            {
                //fetch the page if page url is set
                //if not, go to the next page
                if (page.HasPageUrl)
                {
                    var pageUrl = page.PageUrl;
                    var client = SpotifyClient.Clients[connectionId];
                    var mercury = client.ContextResolver;

                    var pageResolve = mercury.ResolveRaw(pageUrl).ConfigureAwait(false)
                        .GetAwaiter().GetResult();
                    foreach (var track in BuildFutureTracks(connectionId, pageResolve))
                    {
                        yield return track;
                    }
                }
                else if (page.HasNextPageUrl)
                {
                    var pageUrl = page.NextPageUrl;
                    var client = SpotifyClient.Clients[connectionId];
                    var mercury = client.ContextResolver;

                    var pageResolve = mercury.ResolveRaw(pageUrl).ConfigureAwait(false)
                        .GetAwaiter().GetResult();
                    foreach (var track in BuildFutureTracks(connectionId, pageResolve))
                    {
                        yield return track;
                    }
                }
            }
        }
    }
}