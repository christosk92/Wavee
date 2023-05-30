using System.Net.Http.Headers;
using System.Net.Http.Json;
using ReactiveUI;
using Wavee.Spotify;
using Wavee.Spotify.Infrastructure.Playback;
using Wavee.UI.Config;

namespace Wavee.UI.States.Spotify;

public class SpotifyState : ReactiveObject
{
    private static HttpClient _httpClient = new();
    private SpotifyClient? _client;

    public SpotifyState(AppConfig config)
    {
        SpotifyConfig = new SpotifyConfig(
            Remote: new SpotifyRemoteConfig(
                DeviceName: config.SpotifyDeviceName ?? "Wavee",
                DeviceType: config.SpotifyDeviceType
            ),
            Playback: new SpotifyPlaybackConfig(
                PreferredQualityType: PreferredQualityType.Normal,
                CrossfadeDuration: Option<TimeSpan>.None,
                Autoplay: true
            ),
            Cache: new SpotifyCacheConfig(
                CachePath: config.MetadataCachePath,
                AudioCachePath: config.AudioFilesCachePath,
                CacheNoTouchExpiration: Option<TimeSpan>.None
            ),
            locale: "en"
        );
        Instance = this;
    }

    public SpotifyConfig SpotifyConfig { get; }

    public SpotifyClient? Client
    {
        get => _client;
        set => this.RaiseAndSetIfChanged(ref _client, value);
    }
    public static SpotifyState Instance { get; private set; } = null!;

    public Either<NotConnectedResult, TryAsync<T>> GetHttp<T>(string endpoint, CancellationToken ct = default)
    {
        if (Client is null)
        {
            return new NotConnectedResult();
        }

        return TryAsync<T>(async () =>
        {
            var token = await Client.TokenClient.GetToken(ct);
            var authHeader = new AuthenticationHeaderValue("Bearer", token);
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Authorization = authHeader;
            using var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
#pragma warning disable CS8603
            return await response.Content.ReadFromJsonAsync<T>(JsonConverters.SystemText.JsonSerializationOptions.Default.Settings, ct);
#pragma warning restore CS8603
        });
    }
}

public readonly struct NotConnectedResult
{
    public static NotConnectedResult Default = new();
}