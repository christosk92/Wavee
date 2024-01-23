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

        var playContext = Common.CreateContext(connectionId, transferState.CurrentSession.Context);

        if (transferState.Playback?.CurrentTrack is not null)
        {
            //TODO: Skip to track
        }

        await spotifyClient.WaveePlayer.Play(playContext);
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