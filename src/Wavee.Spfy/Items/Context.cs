using LanguageExt;

namespace Wavee.Spfy.Items;

public readonly record struct Context
{
    /// <summary>
    /// The item currently playing in the context.
    /// </summary>
    public required ISpotifyPlayableItem Item { get; init; }

    /// <summary>
    /// Optional: The unique identifier of the item within the context. 
    /// </summary>
    public required Option<string> Uid { get; init; }

    /// <summary>
    /// Optional: The zero-based index of the item within the context, relative to the page (see <see cref="PageIndex"/>)
    /// </summary>
    public required Option<int> ItemIndex { get; init; }

    /// <summary>
    /// Optional: The zero-based index of the page of the items within the context.
    /// </summary>
    public required Option<int> PageIndex { get; init; }
}

public readonly record struct SpotifySimpleContext
{
    public required Option<ISpotifyItem> Item { get; init; }
    public required string Uri { get; init; }
    public required IReadOnlyDictionary<string, string> Metadata { get; init; }
}