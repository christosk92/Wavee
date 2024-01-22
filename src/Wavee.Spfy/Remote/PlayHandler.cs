using System.Diagnostics;
using System.Text.Json;
using Eum.Spotify.context;
using Eum.Spotify.playback;
using Eum.Spotify.transfer;
using Google.Protobuf;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Spfy.Playback;
using Wavee.Spfy.Playback.Contexts;
using PlayOrigin = Eum.Spotify.connectstate.PlayOrigin;

namespace Wavee.Spfy.Remote;

internal static class PlayHandler
{
    public static async Task HandlePlay(JsonElement cmd, Guid instanceId)
    {
        if (!EntityManager.TryGetClient(instanceId, out var spotifyClient))
        {
            Debugger.Break();
            throw new NotSupportedException();
        }

        var contextStr = cmd.GetProperty("context").GetRawText();
        var context = Context.Parser.ParseJson(contextStr);
        var playOrigin = PlayOrigin.Parser.ParseJson(cmd.GetProperty("play_origin").GetRawText());

        var options = cmd.GetProperty("options");
        var skipto = options.GetProperty("skip_to");

        var skipToUid =
            skipto.TryGetProperty("track_uid", out var uid) && !string.IsNullOrEmpty(uid.GetString())
                ? uid.GetString()
                : null;
        var skipToUri =
            skipto.TryGetProperty("track_uri", out var uri) && !string.IsNullOrEmpty(uri.GetString())
                ? uri.GetString()
                : null;

        var skipToIndex =
            skipto.TryGetProperty("track_index", out var skptdx)
                ? skptdx.GetInt32()
                : (int?)null;

        var sessionId = options.GetProperty("session_id").GetString();

        if (spotifyClient.WaveePlayer.Context.IsSome)
        {
            var ctx = spotifyClient.WaveePlayer.Context.ValueUnsafe();
            if (ctx is ISpotifyContext spotifyContext)
            {
                if (spotifyContext.ContextUri == context.Uri)
                {
                    // Skip to a track!!
                    if (skipToIndex.HasValue)
                    {
                        await spotifyClient.WaveePlayer.PlayWithinContext(skipToIndex.Value);
                    }

                    if (!string.IsNullOrEmpty(skipToUid))
                    {
                        
                    }

                    if (!string.IsNullOrEmpty(skipToUri))
                    {
                        
                    }
                    
                    return;
                }
            }
        }

        var playContext = new LazySpotifyContext(instanceId,
            context,
            track => Common.Predicate(track,
                skipToUri,
                ByteString.Empty,
                skipToUid),
            spotifyClient.Playback.CreateSpotifyStream,
            Option<WaveeContextStream>.None);

        await spotifyClient.WaveePlayer.Play(playContext);
    }
}