using Wavee.Core.Enums;
using Wavee.Interfaces;

namespace Wavee.Core.Models;

public readonly record struct WaveePlaybackState
{
    public required IWaveeMediaSource? Track { get; init; }
    public required WaveeRepeatStateType RepeatMode { get; init; }
    public required bool IsShuffling { get; init; }
    public required WaveePlaybackStateType PlaybackState { get; init; }
    public required string PlaybackId { get; init; }
}

public enum WaveeRepeatStateType
{
    None,
    Context,
    Track
}