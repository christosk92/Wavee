using System.Text.Json;
using Wavee.Spotify.Infrastructure.HttpClients;
using Wavee.Spotify.Interfaces;

namespace Wavee.Spotify.Infrastructure.Services;

internal sealed class ApResolverService : IApResolverService
{
    private readonly ApResolverHttpClient _apHttpClient;
    public ApResolverService(ApResolverHttpClient apHttpClient)
    {
        _apHttpClient = apHttpClient;
    }
    public async ValueTask<(string Host, ushort Port)> GetAccessPoint(CancellationToken cancellationToken)
    {
        const string url = "https://apresolve.spotify.com/?type=accesspoint";
        using var response = await _apHttpClient.Client.GetAsync(url, cancellationToken); 
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var jsondoc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var hosts = jsondoc.RootElement.GetProperty("accesspoint");
        using var hostEnumerator = hosts.EnumerateArray();
        hostEnumerator.MoveNext();
        var host = hostEnumerator.Current.GetString();
        //split host into host and port
        var split = host.Split(':');
        var port = ushort.Parse(split[1]);
        return (split[0], port);
    }

    public async ValueTask<(string Host, ushort Port)> GetDealer(CancellationToken cancellationToken)
    {
        const string url = "https://apresolve.spotify.com/?type=dealer";
        using var response = await _apHttpClient.Client.GetAsync(url, cancellationToken); 
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var jsondoc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var hosts = jsondoc.RootElement.GetProperty("dealer");
        using var hostEnumerator = hosts.EnumerateArray();
        hostEnumerator.MoveNext();
        var host = hostEnumerator.Current.GetString();
        //split host into host and port
        var split = host.Split(':');
        var port = ushort.Parse(split[1]);
        return (split[0], port);
    }

    public async Task<string> GetSpClient(CancellationToken cancellationToken)
    {
        const string url = "https://apresolve.spotify.com/?type=spclient";
        using var response = await _apHttpClient.Client.GetAsync(url, cancellationToken); 
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var jsondoc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var hosts = jsondoc.RootElement.GetProperty("spclient");
        using var hostEnumerator = hosts.EnumerateArray();
        hostEnumerator.MoveNext();
        var host = hostEnumerator.Current.GetString();
        //split host into host and port
        var split = host.Split(':');
        var port = ushort.Parse(split[1]);
        return $"https://{split[0]}";
    }
}