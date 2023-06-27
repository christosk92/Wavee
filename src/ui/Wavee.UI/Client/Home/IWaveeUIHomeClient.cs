using Wavee.UI.ViewModel.Home;

namespace Wavee.UI.Client.Home;

public interface IWaveeUIHomeClient
{
    Task<WaveeHome> GetHome(string? filter, CancellationToken ct = default);
}

public sealed class WaveeHome
{
    public required string[] Filters { get; init; } = Array.Empty<string>();
    public required string Greeting { get; init; } = string.Empty;
    public required IEnumerable<HomeGroupSectionViewModel> Sections { get; init; } = Enumerable.Empty<HomeGroupSectionViewModel>();
}