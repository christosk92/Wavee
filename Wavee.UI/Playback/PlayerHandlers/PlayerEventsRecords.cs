using Wavee.Enums;
using Wavee.Interfaces.Models;
using Wavee.UI.Interfaces.Playback;

namespace Wavee.UI.Playback.PlayerHandlers;

public readonly record struct TrackChangedEvent(IPlayableItem Track, int Index) : IPlayerViewModelEvent;
public readonly record struct PausedEvent() : IPlayerViewModelEvent;

public readonly record struct ContextChangedEvent(IPlayContext context) : IPlayerViewModelEvent;
public readonly record struct ResumedEvent() : IPlayerViewModelEvent;

public readonly record struct SeekedEvent(ulong SeekedToMs) : IPlayerViewModelEvent;
public readonly record struct ShuffleToggledEvent(bool shuffling) : IPlayerViewModelEvent;
public readonly record struct RepeatStateChangedEvent(RepeatState state) : IPlayerViewModelEvent;
public readonly record struct VolumeChangedEvent(double Volume) : IPlayerViewModelEvent;