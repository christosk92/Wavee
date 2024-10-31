using Wavee.Interfaces;

namespace Wavee.Services;

internal sealed class SpotifyTimeProvider : ITimeProvider
{
    private TimeSpan? _offset;
    private readonly ISpotifyApiClient _apiClient;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public SpotifyTimeProvider(ISpotifyApiClient apiClient)
    {
        _apiClient = apiClient;

        Task.Run(async () =>
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                await SynchronizeTimeAsync();
                await Task.Delay(TimeSpan.FromMinutes(15), _cancellationTokenSource.Token);
            }
        });
    }

    public ValueTask<DateTimeOffset> CurrentTime()
    {
        if (_offset.HasValue)
        {
            return new ValueTask<DateTimeOffset>(DateTimeOffset.UtcNow + _offset.Value);
        }

        return new ValueTask<DateTimeOffset>(SynchronizeTimeAsync());
    }

    private async Task<DateTimeOffset> SynchronizeTimeAsync()
    {
        var offset = await _apiClient.GetServerTimeOffset(CancellationToken.None);
        _offset = offset;
        return DateTimeOffset.UtcNow + offset;
    }
}