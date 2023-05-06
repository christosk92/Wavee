using System.Diagnostics.CodeAnalysis;
using Eum.Spotify.connectstate;

namespace Wavee.Spotify.Remote.Infrastructure.State;

internal static class RestrictionsManager
{
    public static readonly string REASON_ENDLESS_CONTEXT = "endless_context";
    public static readonly string REASON_NO_PREV_TRACK = "no_prev_track";
    public static readonly string REASON_NO_NEXT_TRACK = "no_next_track";

    public static bool Can(Restrictions restrictions, RestrictionAction restrictionAction)
    {
        return restrictionAction switch
        {
            RestrictionAction.SHUFFLE => restrictions.DisallowTogglingShuffleReasons.Count == 0,
            RestrictionAction.REPEAT_CONTEXT => restrictions.DisallowTogglingRepeatContextReasons.Count == 0,
            RestrictionAction.REPEAT_TRACK => restrictions.DisallowTogglingRepeatTrackReasons.Count == 0,
            RestrictionAction.PAUSE => restrictions.DisallowPausingReasons.Count == 0,
            RestrictionAction.RESUME => restrictions.DisallowResumingReasons.Count == 0,
            RestrictionAction.SEEK => restrictions.DisallowSeekingReasons.Count == 0,
            RestrictionAction.SKIP_PREV => restrictions.DisallowSkippingPrevReasons.Count == 0,
            RestrictionAction.SKIP_NEXT => restrictions.DisallowSkippingNextReasons.Count == 0,
            _ => throw new ArgumentException("Unknown restriction for " + restrictionAction)
        };
    }

    public static void Disallow(Restrictions restrictions, RestrictionAction restrictionAction, string reason)
    {
        switch (restrictionAction)
        {
            case RestrictionAction.SHUFFLE:
                restrictions.DisallowTogglingShuffleReasons.Add(reason);
                break;
            case RestrictionAction.REPEAT_CONTEXT:
                restrictions.DisallowTogglingRepeatContextReasons.Add(reason);
                break;
            case RestrictionAction.REPEAT_TRACK:
                restrictions.DisallowTogglingRepeatTrackReasons.Add(reason);
                break;
            case RestrictionAction.PAUSE:
                restrictions.DisallowPausingReasons.Add(reason);
                break;
            case RestrictionAction.RESUME:
                restrictions.DisallowResumingReasons.Add(reason);
                break;
            case RestrictionAction.SEEK:
                restrictions.DisallowSeekingReasons.Add(reason);
                break;
            case RestrictionAction.SKIP_PREV:
                restrictions.DisallowSkippingPrevReasons.Add(reason);
                break;
            case RestrictionAction.SKIP_NEXT:
                restrictions.DisallowSkippingNextReasons.Add(reason);
                break;
            default:
                throw new ArgumentOutOfRangeException("Unknown restriction for " + restrictionAction);
        }
    }

    public static void Allow(Restrictions restrictions, RestrictionAction restrictionAction)
    {
        switch (restrictionAction)
        {
            case RestrictionAction.SHUFFLE:
                restrictions.DisallowTogglingShuffleReasons.Clear();
                break;
            case RestrictionAction.REPEAT_CONTEXT:
                restrictions.DisallowTogglingRepeatContextReasons.Clear();
                break;
            case RestrictionAction.REPEAT_TRACK:
                restrictions.DisallowTogglingRepeatTrackReasons.Clear();
                break;
            case RestrictionAction.PAUSE:
                restrictions.DisallowPausingReasons.Clear();
                break;
            case RestrictionAction.RESUME:
                restrictions.DisallowResumingReasons.Clear();
                break;
            case RestrictionAction.SEEK:
                restrictions.DisallowSeekingReasons.Clear();
                break;
            case RestrictionAction.SKIP_PREV:
                restrictions.DisallowSkippingPrevReasons.Clear();
                break;
            case RestrictionAction.SKIP_NEXT:
                restrictions.DisallowSkippingNextReasons.Clear();
                break;
            default:
                throw new ArgumentOutOfRangeException("Unknown restriction for " + restrictionAction);
        }
    }

    public static void AllowEverything(Restrictions restritions)
    {
        restritions.DisallowTogglingShuffleReasons.Clear();
        restritions.DisallowTogglingRepeatContextReasons.Clear();
        restritions.DisallowTogglingRepeatTrackReasons.Clear();
        restritions.DisallowPausingReasons.Clear();
        restritions.DisallowResumingReasons.Clear();
        restritions.DisallowSeekingReasons.Clear();
        restritions.DisallowSkippingPrevReasons.Clear();
        restritions.DisallowSkippingNextReasons.Clear();
    }
}