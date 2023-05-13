namespace Wavee.Core.Id;


/// <summary>
/// A unique identifier for an audio item from a source.
/// </summary>
/// <param name="Id">
/// The unique identifier for the audio item.
/// For example, the Spotify track ID or the local file path.
/// </param>
/// <param name="Type">
/// The type of audio item.
/// </param>
/// <param name="Source">
///  The unique identifier for the source of the audio item.
///  
/// </param>
public readonly record struct AudioId(string Id, AudioItemType Type, string Source);