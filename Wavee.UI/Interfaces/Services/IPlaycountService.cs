namespace Wavee.UI.Interfaces.Services;

public interface IPlaycountService
{
    Task IncrementPlayCount(string id, DateTime startedAt, TimeSpan duration, CancellationToken ct = default);
    Task<IEnumerable<DateTime>> GetPlayDates(string id, CancellationToken ct = default);

    Task<IReadOnlyDictionary<string, (int Playcounts, DateTime LastPlayed)>> GetPlaycounts(IList<string> tracks,
        CancellationToken ct = default);

}