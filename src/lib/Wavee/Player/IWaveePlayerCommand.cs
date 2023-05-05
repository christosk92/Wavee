using Wavee.Player.Context;

namespace Wavee.Player;

public interface IWaveePlayerCommand { }

/// <summary>
/// A command to play a context.
/// </summary>
/// <param name="Context">
/// The context to play.
/// </param>
/// <param name="IndexInContext">
/// Maybe the index in the context to start playing from.
/// If this is None, then the player should play from the beginning.
/// Or will shuffle if the context is shuffled.
/// </param>
/// <param name="StartFrom">
/// Maybe the position in the track to start playing from.
/// If this is None, then the player should play from the beginning.
/// </param>
/// <param name="StartPlayback">
/// Maybe whether to start playback.
/// If this is None, then the player should start playback.
/// </param>
public readonly record struct PlayContextCommand(
    IPlayContext Context,
    Option<int> IndexInContext,
    Option<TimeSpan> StartFrom,
    Option<bool> StartPlayback) : IWaveePlayerCommand;
