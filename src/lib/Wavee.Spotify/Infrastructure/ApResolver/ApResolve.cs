using System.Text.Json;
using Wavee.Core.Infrastructure.IO;
using Array = System.Array;

namespace Wavee.Spotify.Infrastructure.ApResolver;

internal static class ApResolve
{
    private static (string Host, ushort Port)[] _dealer = Array.Empty<(string, ushort)>();
    private static (string Host, ushort Port)[] _accessPoint = Array.Empty<(string, ushort)>();
    private static (string Host, ushort Port)[] _spclient = Array.Empty<(string, ushort)>();

    private const string url = "https://apresolve.spotify.com/?type=accesspoint&type=dealer&type=spclient";

    public static async ValueTask<(string host, ushort port)> GetAccessPoint(CancellationToken ct)
    {
        if (_accessPoint.Length == 0)
        {
            await Populate(ct);
        }

        return _accessPoint[0];
    }

    public static async ValueTask<(string host, ushort port)> GetDealer(CancellationToken ct)
    {
        if (_dealer.Length == 0)
        {
            await Populate(ct);
        }

        return _dealer[0];
    }

    public static async ValueTask<(string host, ushort port)> GetSpClient(CancellationToken none)
    {
        if (_spclient.Length == 0)
        {
            await Populate(none);
        }

        return _spclient[0];
    }

    private static async Task Populate(CancellationToken ct)
    {
        using var response = await HttpIO.GetAsync(url, None, Empty, ct);
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var jsonDocument = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var accessPoint = jsonDocument.RootElement.GetProperty("accesspoint");
        var dealers = jsonDocument.RootElement.GetProperty("dealer");
        var spclient = jsonDocument.RootElement.GetProperty("spclient");

        static (string, ushort) Parse(JsonElement elem)
        {
            var str = elem.GetString();
            ReadOnlyMemory<string> split = str.Split(':');
            return (split.Span[0], ushort.Parse(split.Span[1]));
        }

        _accessPoint = new (string, ushort)[accessPoint.GetArrayLength()];
        for (int i = 0; i < _accessPoint.Length; i++)
        {
            _accessPoint[i] = Parse(accessPoint[i]);
        }

        _dealer = new (string, ushort)[dealers.GetArrayLength()];
        for (int i = 0; i < _dealer.Length; i++)
        {
            _dealer[i] = Parse(dealers[i]);
        }

        _spclient = new (string, ushort)[spclient.GetArrayLength()];
        for (int i = 0; i < _spclient.Length; i++)
        {
            _spclient[i] = Parse(spclient[i]);
        }
    }
}