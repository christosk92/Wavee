using System.Diagnostics;
using Eum.Spotify.transfer;
using Google.Protobuf;
using LanguageExt;
using Spotify.Metadata;
using Wavee.Contexting;
using Wavee.Spfy.Playback;
using Wavee.Spfy.Playback.Contexts;

namespace Wavee.Spfy.Remote;

internal static class RemoteTransfer
{
    public static async Task HandleTransfer(TransferState transferState,
        Guid connectionId)
    {
        // Fastest transfer:
        // Create a lazy context, but play the first track immediatly in WaveePlayer
        if (!EntityManager.TryGetClient(connectionId, out var spotifyClient))
        {
            Debugger.Break();
            throw new NotSupportedException();
        }

        if (transferState.Playback?.CurrentTrack is not null)
        {
            // TODO: Episode
            var trackId = SpotifyId.FromRaw(transferState.Playback.CurrentTrack.Gid.Span, AudioItemType.Track);

            var stream = await spotifyClient.Playback.CreateSpotifyStream(trackId, CancellationToken.None);
            var ctxStream = new WaveeContextStream(stream,
                Common.ConstructComposedKeyForCurrentTrack(transferState.Playback, trackId));
            var x = BuildContext(transferState, connectionId, spotifyClient.Playback.CreateSpotifyStream, ctxStream);
            await spotifyClient.WaveePlayer.Play(ctxStream, x);
            return;
        }

        // just play the first track
        // TODO:
        var context = BuildContext(transferState, connectionId, spotifyClient.Playback.CreateSpotifyStream,
            Option<WaveeContextStream>.None);
        await spotifyClient.WaveePlayer.Play(context);
    }

    private static SpotifyRealContext BuildContext(TransferState transferState,
        Guid connectionId,
        Func<SpotifyId, CancellationToken, Task<WaveeStream>> streamFactory,
        Option<WaveeContextStream> firstStream)
    {
        return new LazySpotifyContext(connectionId,
            transferState.CurrentSession.Context,
            track => Common.Predicate(track, 
                transferState.Playback?.CurrentTrack?.Uri,
                transferState.Playback?.CurrentTrack?.Gid ?? ByteString.Empty,
                transferState.Playback?.CurrentTrack?.Uid),
            streamFactory,
            firstStream);
    }
}

// internal sealed class LazySpotifyContext : SpotifyRealContext
// {
//     public LazySpotifyContext(Guid connectionId, SpotifyId itemId, Option<int> startIndex, Func<SpotifyId, CancellationToken, Task<WaveeStream>> streamFactory) : base(connectionId, itemId, startIndex, streamFactory)
//     {
//     }
//
//     public override HashMap<string, string> ContextMetadata { get; }
//     protected override ValueTask<Option<SpotifyContextPage>> NextPage()
//     {
//         throw new NotImplementedException();
//     }
// }