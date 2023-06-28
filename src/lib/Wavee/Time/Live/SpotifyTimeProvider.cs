using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using LanguageExt;
using Serilog;

namespace Wavee.Time.Live;

internal sealed class SpotifyTimeProvider : ITimeProvider, IDisposable
{
    private readonly Timer _timer;
    private readonly TimeSyncMethod _timeSyncMethod;
    private int _measuredOffset;
    private readonly Func<CancellationToken, ValueTask<string>> _tokenFactory;

    public SpotifyTimeProvider(TimeSyncMethod timeSyncMethod,
        Func<CancellationToken, ValueTask<string>> tokenFactory,
        Option<int> correctionifManual)
    {
        _timeSyncMethod = timeSyncMethod;
        _tokenFactory = tokenFactory;
        _measuredOffset = correctionifManual.IfNone(0);

        if (timeSyncMethod is not TimeSyncMethod.Manual)
        {
            _timer = new Timer(OnTimer, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        }
    }

    public long CurrentTimeMilliseconds => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + _measuredOffset;
    public int Offset => _measuredOffset;

    private async void OnTimer(object state)
    {
        switch (_timeSyncMethod)
        {
            case TimeSyncMethod.Ntp:
                await UpdateWithNtp();
                break;
            case TimeSyncMethod.Melody:
                await UpdateWithMelody();
                break;
        }
    }

    private static readonly HttpClient _melodyHttpClient = new HttpClient();
    private async Task UpdateWithMelody()
    {
        const string spclient = "gae2-spclient.spotify.com:443";
        const string url = $"https://{spclient}/melody/v1/time";

        int minusSomething = 0;
        //lets do a head request first;
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        try
        {
            var tokenSw = Stopwatch.StartNew();
            using var request = new HttpRequestMessage(HttpMethod.Options, url);
            var token = await _tokenFactory(CancellationToken.None);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            tokenSw.Stop();
            minusSomething = (int)tokenSw.ElapsedMilliseconds;
            using var response = await _melodyHttpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed notifying server of time request. Retrying in 10 seconds");
            //change timer to 10 seconds
            _timer.Change(TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(1));
            return;
        }

        //now do a get request
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            var tokenSw = Stopwatch.StartNew();
            var token = await _tokenFactory(CancellationToken.None);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            tokenSw.Stop();
            minusSomething += (int)tokenSw.ElapsedMilliseconds;

            using var response = await _melodyHttpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead);
            response.EnsureSuccessStatusCode();

            var deserializationTime = Stopwatch.StartNew();
            using var stream = await response.Content.ReadAsStreamAsync();
            using var jsondocument = await JsonDocument.ParseAsync(stream);
            var serverTime = jsondocument.RootElement.GetProperty("timestamp").GetInt64();
            deserializationTime.Stop();
            minusSomething += (int)deserializationTime.ElapsedMilliseconds;
            var diff = serverTime - now - minusSomething;
            _measuredOffset = (int)diff;

            Log.Information("Measured offset: {Offset}ms. Took {Time}ms to get token and {DeserializationTime}ms to deserialize", diff, minusSomething, deserializationTime.ElapsedMilliseconds);
            _timer.Change(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed getting time from server. Retrying in 10 seconds");
            //change timer to 10 seconds
            _timer.Change(TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(1));
            return;
        }
    }

    private async Task UpdateWithNtp()
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}

public enum TimeSyncMethod
{
    /// <summary>
    /// Measures the offset between the local system time a NTP server.
    /// </summary>
    Ntp,
    /// <summary>
    /// Measures the offset between the local system time and the Spotify server time (recommended).
    /// </summary>
    Melody,
    /// <summary>
    /// Measures the offset between the local system time and the Spotify server time.
    /// </summary>
    Manual
}