namespace Wavee.Player;

/// <summary>
/// An enum representing the repeat state of a player.
/// </summary>
public enum RepeatState
{
    /// <summary>
    /// The player is not repeating.
    /// </summary>
    Off = 0,
    
    /// <summary>
    /// The player is repeating the current context (album, playlist etc.)
    /// </summary>
    Context = 1,
    
    /// <summary>
    /// The player is repeating specifically the current track
    /// </summary>
    Track = 2
}