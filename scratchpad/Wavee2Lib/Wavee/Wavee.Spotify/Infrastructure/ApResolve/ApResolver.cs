using System.Net.Http.Headers;
using LanguageExt;
using Wavee.Infrastructure.IO;

namespace Wavee.Spotify.Infrastructure.ApResolve;

internal static class ApResolver
{
    public static Option<string> AccessPoint { get; private set; }
    public static Option<string> SpClient { get; private set; }
    public static Option<string> Dealer { get; private set; }


    public static async Task Populate()
    {
        //https://apresolve.spotify.com/?type=accesspoint&type=dealer&type=spclient
        const string url = "https://apresolve.spotify.com/?type=accesspoint&type=dealer&type=spclient";
        var response = await HttpIO.GetJsonAsync<ApResolveResponse>(url,
            Option<AuthenticationHeaderValue>.None, CancellationToken.None);

        AccessPoint = response.accesspoint.First();
        Dealer = response.dealer.First();
        SpClient = response.spclient.First();
    }
    
    private record ApResolveResponse(IEnumerable<string> accesspoint, IEnumerable<string> dealer, IEnumerable<string> spclient);
}