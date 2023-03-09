namespace Wavee.UI.ViewModels.Playback.PlayerEvents;
public readonly record struct TrackChangedEvent() : IPlayerViewModelEvent;

public readonly record struct PausedEvent() : IPlayerViewModelEvent;

public readonly record struct ResumedEvent() : IPlayerViewModelEvent;

public readonly record struct SeekedEvent(ulong SeekedToMs) : IPlayerViewModelEvent;