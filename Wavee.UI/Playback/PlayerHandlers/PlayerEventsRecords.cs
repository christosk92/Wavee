using Wavee.Interfaces.Models;

namespace Wavee.UI.Playback.PlayerHandlers;

public readonly record struct TrackChangedEvent(IPlayableItem Track) : IPlayerViewModelEvent;

public readonly record struct PausedEvent() : IPlayerViewModelEvent;

public readonly record struct ResumedEvent() : IPlayerViewModelEvent;

public readonly record struct SeekedEvent(ulong SeekedToMs) : IPlayerViewModelEvent;