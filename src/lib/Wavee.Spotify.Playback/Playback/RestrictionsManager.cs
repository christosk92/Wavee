using System.Diagnostics.CodeAnalysis;
using Eum.Spotify.connectstate;

namespace Wavee.Spotify.Playback.Playback;

internal class RestrictionsManager
{
    public static readonly string REASON_ENDLESS_CONTEXT = "endless_context";
    public static readonly string REASON_NO_PREV_TRACK = "no_prev_track";
    public static readonly string REASON_NO_NEXT_TRACK = "no_next_track";
    private readonly Restrictions _restrictions;

    public RestrictionsManager()
    {
        _restrictions = new Restrictions();
    }

    public bool Can([NotNull] Action action)
    {
        return action switch
        {
            Action.SHUFFLE => _restrictions.DisallowTogglingShuffleReasons.Count == 0,
            Action.REPEAT_CONTEXT => _restrictions.DisallowTogglingRepeatContextReasons.Count == 0,
            Action.REPEAT_TRACK => _restrictions.DisallowTogglingRepeatTrackReasons.Count == 0,
            Action.PAUSE => _restrictions.DisallowPausingReasons.Count == 0,
            Action.RESUME => _restrictions.DisallowResumingReasons.Count == 0,
            Action.SEEK => _restrictions.DisallowSeekingReasons.Count == 0,
            Action.SKIP_PREV => _restrictions.DisallowSkippingPrevReasons.Count == 0,
            Action.SKIP_NEXT => _restrictions.DisallowSkippingNextReasons.Count == 0,
            _ => throw new ArgumentException("Unknown restriction for " + action)
        };
    }


    public Restrictions ToProto() => _restrictions;
    public void Disallow([NotNull] Action action, [NotNull] String reason)
    {
        Allow(action);
        switch (action)
        {
            case Action.SHUFFLE:
                _restrictions.DisallowTogglingShuffleReasons.Add(reason);
                break;
            case Action.REPEAT_CONTEXT:
                _restrictions.DisallowTogglingRepeatContextReasons.Add(reason);
                break;
            case Action.REPEAT_TRACK:
                _restrictions.DisallowTogglingRepeatTrackReasons.Add(reason);
                break;
            case Action.PAUSE:
                _restrictions.DisallowPausingReasons.Add(reason);
                break;
            case Action.RESUME:
                _restrictions.DisallowResumingReasons.Add(reason);
                break;
            case Action.SEEK:
                _restrictions.DisallowSeekingReasons.Add(reason);
                break;
            case Action.SKIP_PREV:
                _restrictions.DisallowSkippingPrevReasons.Add(reason);
                break;
            case Action.SKIP_NEXT:
                _restrictions.DisallowSkippingNextReasons.Add(reason);
                break;
            default:
                throw new ArgumentOutOfRangeException("Unknown restriction for " + action);
        }
    }

    public void Allow([NotNull] Action action)
    {
        switch (action)
        {
            case Action.SHUFFLE:
                _restrictions.DisallowTogglingShuffleReasons.Clear();
                break;
            case Action.REPEAT_CONTEXT:
                _restrictions.DisallowTogglingRepeatContextReasons.Clear();
                break;
            case Action.REPEAT_TRACK:
                _restrictions.DisallowTogglingRepeatTrackReasons.Clear();
                break;
            case Action.PAUSE:
                _restrictions.DisallowPausingReasons.Clear();
                break;
            case Action.RESUME:
                _restrictions.DisallowResumingReasons.Clear();
                break;
            case Action.SEEK:
                _restrictions.DisallowSeekingReasons.Clear();
                break;
            case Action.SKIP_PREV:
                _restrictions.DisallowSkippingPrevReasons.Clear();
                break;
            case Action.SKIP_NEXT:
                _restrictions.DisallowSkippingNextReasons.Clear();
                break;
            default:
                throw new ArgumentOutOfRangeException("Unknown restriction for " + action);
        }
    }

    public enum Action
    {
        SHUFFLE,
        REPEAT_CONTEXT,
        REPEAT_TRACK,
        PAUSE,
        RESUME,
        SEEK,
        SKIP_PREV,
        SKIP_NEXT
    }
}