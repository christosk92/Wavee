using Wavee.Id;

namespace Wavee.Metadata.Home;

public sealed class SpotifyHomeGroupSection
{
    public required string? Title { get; init; }
    public required SpotifyId SectionId { get; init; }
    public required uint TotalCount { get; init; }
    public required IEnumerable<ISpotifyHomeItem> Items { get; init; }
}