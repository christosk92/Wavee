namespace Wavee.Core;

/// <summary>
/// An enum representing the repeat state of the player.
/// </summary>
public enum RepeatState
{
    /// <summary>
    /// No repeat state. Playback will stop after the end of the context. Or in the case of autoplay, will start a new autoplay context.
    /// </summary>
    None,

    /// <summary>
    /// Repeat the current context (e.g. album, playlist, or episode) after playback ends. Regardless of autoplay settings.
    /// </summary>
    Context,

    /// <summary>
    /// Repeat the current track after playback ends. Regardless of autoplay settings.
    /// </summary>
    Track
}