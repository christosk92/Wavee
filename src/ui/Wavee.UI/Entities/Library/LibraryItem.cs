namespace Wavee.UI.Entities.Library;

public sealed class LibraryItem<T> 
{
    public required T Item { get; init; }
    public required DateTimeOffset AddedAt { get; init; }
}