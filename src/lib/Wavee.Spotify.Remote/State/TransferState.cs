using System.Security.Cryptography;
using System.Text.Json;
using Eum.Spotify.connectstate;
using Wavee.Enums;
using Wavee.Spotify.Models;
using Wavee.Spotify.Remote.Infrastructure.Sys;
using Wavee.Spotify.Remote.Infrastructure.Traits;

namespace Wavee.Spotify.Remote.State;

internal static class TransferState
{
    public static Eff<RT, SpotifyRemoteState<RT>> OnTransfer<RT>(this Eum.Spotify.transfer.TransferState state,
        Ref<SpotifyRemoteState<RT>> remoteState, JsonElement jsonElement)
        where RT : struct, HasTime<RT>
    {
        var shufflingContext = state.Options.ShufflingContext;
        var repeatingContext = state.Options.RepeatingContext;
        var repeatingTrack = state.Options.RepeatingTrack;

        var session = state.CurrentSession;

        var playOrigin = new PlayOrigin
        {
            FeatureIdentifier = session.PlayOrigin.HasFeatureIdentifier
                ? session.PlayOrigin.FeatureIdentifier
                : string.Empty,
            FeatureVersion = session.PlayOrigin.HasFeatureVersion ? session.PlayOrigin.FeatureVersion : string.Empty,
            ViewUri = session.PlayOrigin.HasViewUri ? session.PlayOrigin.ViewUri : string.Empty,
            ReferrerIdentifier = session.PlayOrigin.HasReferrerIdentifier
                ? session.PlayOrigin.ReferrerIdentifier
                : string.Empty,
            ExternalReferrer = session.PlayOrigin.HasExternalReferrer
                ? session.PlayOrigin.ExternalReferrer
                : string.Empty,
            DeviceIdentifier = session.PlayOrigin.HasDeviceIdentifier
                ? session.PlayOrigin.DeviceIdentifier
                : string.Empty,
        };
        foreach (var classe in session.PlayOrigin.FeatureClasses)
        {
            playOrigin.FeatureClasses.Add(classe);
        }

        var context = session.Context;
        var contextUri = context.HasUri ? Some(context.Uri) : None;
        var contextUrl = context.HasUrl ? Some(context.Url) : string.Empty;
        var restrictions = context.Restrictions;

        var playback = state.Playback;
        var timestamp = playback.Timestamp;
        var positionAsOfTimestamp = playback.PositionAsOfTimestamp;
        var playbackSpeed = playback.PlaybackSpeed;
        var isPaused = playback.IsPaused;
        var currentTrack = playback.CurrentTrack;
        var trackId = SpotifyId.FromRaw(currentTrack.Gid.Span, AudioItemType.Track);
        var trackMetadata = currentTrack.Metadata;

        var swapped = remoteState.SwapEff(spotifyRemoteState =>
        {
            return Eff<RT, SpotifyRemoteState<RT>>(rt =>
            {
                var ts = isPaused
                    ? Time<RT>.timestamp.Run(rt).Match(Succ: arg => arg, Fail: _ => throw new Exception())
                    : (ulong)timestamp;

                return spotifyRemoteState with
                {
                    PositionAsOfTimestamp = Some(positionAsOfTimestamp),
                    Timestamp = ts,
                    Position = 0,
                    SessionId = GenerateSessionId(),
                    PlayOrigin = playOrigin,
                    ShufflingContext = shufflingContext,
                    RepeatingContext = repeatingContext,
                    RepeatingTrack = repeatingTrack,
                    IsPaused = isPaused,
                    StartedPlayingAt = ts,
                    IsPlaying = true,
                    ContextUri = contextUri,
                    ContextUrl = contextUrl,
                    TrackId = trackId,
                };
            });
        });
        return swapped;
    }

    private static string GenerateSessionId()
    {
        Span<byte> bytes = new byte[16];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace("-", "")
            .Replace('+', '_') // replace URL unsafe characters with safe ones
            .Replace('/', '_') // replace URL unsafe characters with safe ones
            .Replace("=", ""); // no padding
    }
}