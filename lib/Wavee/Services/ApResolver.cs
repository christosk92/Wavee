using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Wavee.Services;

internal sealed class ApResolver
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApResolver> _logger;
    private AccessPoints _data;

    public ApResolver(HttpClient httpClient, ILogger<ApResolver> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _data = new AccessPoints();
    }

    public async Task<(string Host, int Port)> ResolveAsync(string endpoint,
        CancellationToken cancellationToken = default)
    {
        if (IsAnyEmpty())
        {
            await ApResolveAsync(cancellationToken);
        }

        var socketAddr = endpoint switch
        {
            "accesspoint" => _data.AccessPoint.Dequeue(),
            "dealer" => _data.Dealer.Dequeue(),
            "spclient" => _data.SpClient.Dequeue(),
            _ => throw new NotImplementedException($"No implementation to resolve access point {endpoint}")
        };
        return (socketAddr.Host, socketAddr.Port);
    }

    private bool IsAnyEmpty()
    {
        return _data.IsAnyEmpty();
    }

    private async Task ApResolveAsync(CancellationToken cancellationToken = default)
    {
        var result = await TryApResolveAsync(cancellationToken);

        if (result != null)
        {
            _data = ParseResolveToAccessPoints(result);
        }
        else
        {
            _logger.LogWarning("Failed to resolve all access points, using fallbacks");
            _data = ParseResolveToAccessPoints(ApResolveData.Fallback());
        }

        if (_data.IsAnyEmpty())
        {
            var fallback = ParseResolveToAccessPoints(ApResolveData.Fallback());
            _data.Merge(fallback);
        }
    }

    private async Task<ApResolveData> TryApResolveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                "https://apresolve.spotify.com/?type=accesspoint&type=dealer&type=spclient");
            var response = await _httpClient.SendAsync(request, cancellationToken);

            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<ApResolveData>(responseBody, SpotifyClient.DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to resolve access points: {ex.Message}");
            return null;
        }
    }

    private AccessPoints ParseResolveToAccessPoints(ApResolveData resolveData)
    {
        return new AccessPoints
        {
            AccessPoint = ProcessApStrings(resolveData.AccessPoint),
            Dealer = ProcessApStrings(resolveData.Dealer),
            SpClient = ProcessApStrings(resolveData.SpClient)
        };
    }

    private Queue<SocketAddress> ProcessApStrings(List<string> data)
    {
        var result = new Queue<SocketAddress>();
        var filterPort = PortConfig();

        foreach (var ap in data)
        {
            var parts = ap.Split(':');
            if (parts.Length != 2 || !int.TryParse(parts[1], out var port))
            {
                continue;
            }

            if (!filterPort.HasValue || filterPort == port)
            {
                result.Enqueue(new SocketAddress(parts[0], port));
            }
        }

        return result;
    }

    private int? PortConfig()
    {
        //TODO: Proxy
        return null;
        // return _session.Config.Proxy != null || _session.Config.ApPort.HasValue
        //     ? _session.Config.ApPort ?? 443
        //     : (int?)null;
    }


    private class AccessPoints
    {
        public Queue<SocketAddress> AccessPoint { get; set; } = new Queue<SocketAddress>();
        public Queue<SocketAddress> Dealer { get; set; } = new Queue<SocketAddress>();
        public Queue<SocketAddress> SpClient { get; set; } = new Queue<SocketAddress>();

        public bool IsAnyEmpty()
        {
            return AccessPoint.Count == 0 || Dealer.Count == 0 || SpClient.Count == 0;
        }

        public void Merge(AccessPoints fallback)
        {
            while (fallback.AccessPoint.Count > 0)
            {
                AccessPoint.Enqueue(fallback.AccessPoint.Dequeue());
            }

            while (fallback.Dealer.Count > 0)
            {
                Dealer.Enqueue(fallback.Dealer.Dequeue());
            }

            while (fallback.SpClient.Count > 0)
            {
                SpClient.Enqueue(fallback.SpClient.Dequeue());
            }
        }
    }

    internal class ApResolveData
    {
        [JsonPropertyName("accesspoint")] public List<string> AccessPoint { get; set; } = new List<string>();
        [JsonPropertyName("dealer")] public List<string> Dealer { get; set; } = new List<string>();
        [JsonPropertyName("spclient")] public List<string> SpClient { get; set; } = new List<string>();

        public static ApResolveData Fallback()
        {
            return new ApResolveData
            {
                AccessPoint = new List<string> { "ap.spotify.com:443" },
                Dealer = new List<string> { "dealer.spotify.com:443" },
                SpClient = new List<string> { "spclient.wg.spotify.com:443" }
            };
        }
    }

    private class SocketAddress
    {
        public string Host { get; }
        public int Port { get; }

        public SocketAddress(string host, int port)
        {
            Host = host;
            Port = port;
        }

        public override string ToString()
        {
            return $"{Host}:{Port}";
        }
    }
}