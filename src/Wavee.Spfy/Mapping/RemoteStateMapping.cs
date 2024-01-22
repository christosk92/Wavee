using System.Collections.Immutable;
using System.Diagnostics;
using System.Net;
using Eum.Spotify.connectstate;
using Google.Protobuf.Collections;
using LanguageExt;
using LanguageExt.Common;
using Wavee.Spfy.Items;
using Wavee.Spfy.Remote;
using static LanguageExt.Prelude;

namespace Wavee.Spfy.Mapping;

internal static class RemoteStateMapping
{
    public static async ValueTask<Result<SpotifyRemoteState>> ToRemoteState(this Cluster cluster, Guid connectionId)
    {
        if (!EntityManager.TryGetClient(connectionId, out var connectionClient))
            throw new InvalidOperationException("Connection not found");
        
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        try
        {
            var item = new Option<Context>();
            var context = new Option<SpotifySimpleContext>();
            // var restrictions =
            //     new Dictionary<SpotifyRestrictionAppliesForType,
            //         ImmutableArray<SpotifyKnownRestrictionType>>();
            HashMap<SpotifyRestrictionAppliesForType, Seq<SpotifyKnownRestrictionType>> restrictions =
                LanguageExt.HashMap<SpotifyRestrictionAppliesForType, Seq<SpotifyKnownRestrictionType>>.Empty;

            bool isPaused = false;
            WaveeRepeatStateType repeatState = WaveeRepeatStateType.None;
            bool isShuffling = false;
            TimeSpan offset = TimeSpan.Zero;

            var metadataClient = connectionClient.Metadata;

            if (cluster?.PlayerState is { } playerState)
            {
                if (!string.IsNullOrEmpty(playerState.Track?.Uri)
                    && SpotifyId.TryParse(playerState.Track.Uri, out var spotifyId))
                {
                    if (spotifyId.IsLocal)
                    {
                        var id = spotifyId.ToString().Split(":")[2..];
                        //spotify:local:ARTIST:ALBUM:TRACK:LENGTH_IN_SECONDS
                        var artist = WebUtility.UrlDecode(id[0]);
                        var album = WebUtility.UrlDecode(id[1]);
                        var track = WebUtility.UrlDecode(id[2]);
                        var duration = TimeSpan.FromSeconds(int.Parse(id[3]));
                        
                        item = Some(new Context
                        {
                            Item = new SpotifySimpleTrack
                            {
                                Uri = spotifyId,
                                Name = track,
                                DiscNumber = 0,
                                TrackNumber = 0,
                                Descriptions = default,
                                Group = null,
                                AudioFiles = default,
                                PreviewFiles = default,
                                Duration = default,
                                Explicit = false
                            },
                            Uid = !string.IsNullOrEmpty(playerState.Track.Uid)
                                ? Some(playerState.Track.Uid)
                                : new Option<string>(),
                            ItemIndex = playerState.Index is { } index ? Some((int)index.Track) : new Option<int>(),
                            PageIndex = playerState.Index is { } pgIndex
                                ? Some((int)pgIndex.Page)
                                : new Option<int>()
                        });
                    }
                    else
                    {
                        var fetchItem = await metadataClient.GetItem(spotifyId, true);
                        item = Some(new Context
                        {
                            Item = (fetchItem as ISpotifyPlayableItem)!,
                            Uid = !string.IsNullOrEmpty(playerState.Track.Uid)
                                ? Some(playerState.Track.Uid)
                                : new Option<string>(),
                            ItemIndex = playerState.Index is { } index ? Some((int)index.Track) : new Option<int>(),
                            PageIndex = playerState.Index is { } pgIndex
                                ? Some((int)pgIndex.Page)
                                : new Option<int>()
                        });
                    }
                }

                if (playerState.ContextUri.StartsWith("spotify:user:") &&
                    playerState.ContextUri.EndsWith(":collection"))
                {
                    context = Some(new SpotifySimpleContext
                    {
                        Item = None,
                        Uri = playerState.ContextUri,
                        Metadata = playerState.ContextMetadata
                    });
                }
                else if (!string.IsNullOrEmpty(playerState.ContextUri)
                         && SpotifyId.TryParse(playerState.ContextUri, out var contextId))
                {
                    if (contextId.Type is not AudioItemType.Playlist and not AudioItemType.Unknown)
                    {
                        var fetchContext = await metadataClient.GetItem(contextId, true);
                        context = Some(new SpotifySimpleContext
                        {
                            Item = Some(fetchContext),
                            Uri = playerState.ContextUri,
                            Metadata = playerState.ContextMetadata
                        });
                    }
                    else
                    {
                        context = Some(new SpotifySimpleContext
                        {
                            Item = Option<ISpotifyItem>.None,
                            Uri = playerState.ContextUri,
                            Metadata = playerState.ContextMetadata
                        });
                    }
                }
                else if (!string.IsNullOrEmpty(playerState.ContextUri))
                {
                    context = Some(new SpotifySimpleContext
                    {
                        Item = None,
                        Uri = playerState.ContextUri,
                        Metadata = playerState.ContextMetadata
                    });
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

                static Seq<SpotifyKnownRestrictionType> MutateTo(RepeatedField<string> f)
                {
                    return f.Select(x =>
                    {
                        return x switch
                        {
                            "not_paused" => SpotifyKnownRestrictionType.NotPaused,
                            "endless_context" => SpotifyKnownRestrictionType.EndlessContext,
                            "dj-disallow" => SpotifyKnownRestrictionType.Dj,
                            "narration" => SpotifyKnownRestrictionType
                                .Narration,
                            "no_prev_track" => SpotifyKnownRestrictionType.NoPreviousTrack,
                            "radio" => SpotifyKnownRestrictionType
                                .Radio,
                            "autoplay" => SpotifyKnownRestrictionType.AutoPlay,
                            "not_playing_media" => SpotifyKnownRestrictionType.NotPlayingMedia,
                            _ => SpotifyKnownRestrictionType.Unknown
                        };
                    }).ToSeq();
                }

                if (playerState.Restrictions.DisallowPausingReasons.Count > 0)
                {
                    restrictions = restrictions.Add(SpotifyRestrictionAppliesForType.Pausing,
                        MutateTo(playerState.Restrictions.DisallowPausingReasons));
                }

                if (playerState.Restrictions.DisallowResumingReasons.Count > 0)
                {
                    restrictions = restrictions.Add(SpotifyRestrictionAppliesForType.Resuming,
                        MutateTo(playerState.Restrictions.DisallowResumingReasons));
                }

                if (playerState.Restrictions.DisallowSeekingReasons.Count > 0)
                {
                    restrictions = restrictions.Add(SpotifyRestrictionAppliesForType.Seeking,
                        MutateTo(playerState.Restrictions.DisallowSeekingReasons));
                }

                if (playerState.Restrictions.DisallowSkippingNextReasons.Count > 0)
                {
                    restrictions = restrictions.Add(SpotifyRestrictionAppliesForType.SkippingNext,
                        MutateTo(playerState.Restrictions.DisallowSkippingNextReasons));
                }

                if (playerState.Restrictions.DisallowSkippingPrevReasons.Count > 0)
                {
                    restrictions = restrictions.Add(SpotifyRestrictionAppliesForType.SkippingPrevious,
                        MutateTo(playerState.Restrictions.DisallowSkippingPrevReasons));
                }

                if (playerState.Restrictions.DisallowTogglingRepeatContextReasons.Count > 0)
                {
                    restrictions = restrictions.Add(SpotifyRestrictionAppliesForType.RepeatContext,
                        MutateTo(playerState.Restrictions.DisallowTogglingRepeatContextReasons));
                }

                if (playerState.Restrictions.DisallowTogglingRepeatTrackReasons.Count > 0)
                {
                    restrictions = restrictions.Add(SpotifyRestrictionAppliesForType.RepeatTrack,
                        MutateTo(playerState.Restrictions.DisallowTogglingRepeatTrackReasons));
                }

                if (playerState.Restrictions.DisallowTogglingShuffleReasons.Count > 0)
                {
                    restrictions = restrictions.Add(SpotifyRestrictionAppliesForType.Shuffle,
                        MutateTo(playerState.Restrictions.DisallowTogglingShuffleReasons));
                }
                
                var timestamp = playerState.Timestamp;
                var positionAsOfTimestamp = playerState.PositionAsOfTimestamp;
                var diff = now - timestamp;
                offset = TimeSpan.FromMilliseconds(positionAsOfTimestamp + diff);
            }


            return new SpotifyRemoteState
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
                Restrictions = restrictions,
                Devices = cluster?.Device?.Select(x => x.Value)?
                    .Where(x => x.DeviceId != connectionClient._deviceId)
                    .ToSeq() ?? LanguageExt.Seq<DeviceInfo>.Empty,
                ActiveDeviceId = !string.IsNullOrEmpty(cluster?.ActiveDeviceId)
                    ? Some(cluster.ActiveDeviceId)
                    : None
            };
        }
        catch (Exception e)
        {
            return new Result<SpotifyRemoteState>(e);
        }
    }
}