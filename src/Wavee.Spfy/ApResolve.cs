namespace Wavee.Spfy;

internal static class ApResolve
{
    private static (string Host, ushort Port)? _accessPoint;
    private static string? _dealer;
    private static string? _spclient;

    public static ValueTask<(string Host, ushort Port)> GetAccessPoint(IHttpClient httpClient)
    {
        if (_accessPoint is not null) return new ValueTask<(string Host, ushort Port)>(_accessPoint.Value);

        return new ValueTask<(string Host, ushort Port)>(FetchAndSetAccessPoint(httpClient));
    }

    public static ValueTask<string> GetDealer(IHttpClient httpClient)
    {
        if (_dealer is not null) return new ValueTask<string>(_dealer);

        return new ValueTask<string>(FetchAndSetDealer(httpClient));
    }

    public static ValueTask<string> GetSpClient(IHttpClient httpClient)
    {
        if (_spclient is not null) return new ValueTask<string>(_spclient);

        return new ValueTask<string>(FetchAndSetSpClient(httpClient));
    }

    private static async Task<(string Host, ushort Port)> FetchAndSetAccessPoint(IHttpClient httpClient)
    {
        await FetchAndSetAll(httpClient);
        return _accessPoint!.Value;
    }

    private static async Task<string> FetchAndSetDealer(IHttpClient httpClient)
    {
        await FetchAndSetAll(httpClient);
        return _dealer!;
    }

    private static async Task<string> FetchAndSetSpClient(IHttpClient httpClient)
    {
        await FetchAndSetAll(httpClient);
        return _spclient!;
    }

    private static async Task FetchAndSetAll(IHttpClient httpClient)
    {
        var (ap, dealer, sp) = await httpClient.FetchBestAccessPoints();
        var hostAndPortForAp = ap.Split(":");
        _accessPoint = (hostAndPortForAp[0], ushort.Parse(hostAndPortForAp[1]));
        _dealer = dealer;
        _spclient = sp;
    }

    /*
     *     /*
     * {
    "accesspoint": [
        "ap-gae2.spotify.com:4070",
        "ap-gae2.spotify.com:443",
        "ap-gae2.spotify.com:80",
        "ap-gew1.spotify.com:4070",
        "ap-guc3.spotify.com:443",
        "ap-gue1.spotify.com:80"
    ],
    "dealer": [
        "gae2-dealer.spotify.com:443",
        "gew1-dealer.spotify.com:443",
        "guc3-dealer.spotify.com:443",
        "gue1-dealer.spotify.com:443"
    ],
    "spclient": [
        "gae2-spclient.spotify.com:443",
        "gew1-spclient.spotify.com:443",
        "guc3-spclient.spotify.com:443",
        "gue1-spclient.spotify.com:443"
    ]
}
     * /
     */
}