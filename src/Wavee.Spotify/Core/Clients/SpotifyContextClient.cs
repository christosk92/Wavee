using System.Text;
using Eum.Spotify.context;
using Wavee.Spotify.Core.Models.Common;
using Wavee.Spotify.Infrastructure.Context;
using Wavee.Spotify.Infrastructure.HttpClients;
using Wavee.Spotify.Infrastructure.Services;
using Wavee.Spotify.Interfaces;

namespace Wavee.Spotify.Core.Clients;

internal sealed class SpotifyContextClient : ISpotifyContextClient
{
    private readonly ISpotifyTokenService _tokenService;
    private readonly SpotifyInternalHttpClient _httpClient;

    public SpotifyContextClient(ISpotifyTokenService tokenService, SpotifyInternalHttpClient httpClient)
    {
        _tokenService = tokenService;
        _httpClient = httpClient;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public async Task<Context> ResolveContext(string contextId, CancellationToken cancellationToken)
    {
        //https://gae2-spclient.spotify.com/context-resolve/v1/spotify:artist:0oSGxfWSnnOXhD2fKuz2Gy?clientid=d8a5ed958d274c2e8ee717e6a4b0971d&extra=&include_video=true
        var url = $"/context-resolve/v1/{contextId}";
        using var response = await _httpClient.Get(url, "application/protobuf", cancellationToken: cancellationToken);
        var stream = await response.Content.ReadAsStringAsync(cancellationToken);
        var context = Context.Parser.ParseJson(stream);
        return context;
    }

    public Task<Context> ResolveArtistContext(SpotifyId artistId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<ContextPage> ResolveContextRaw(string pageUrl, CancellationToken cancellationToken)
    {
        //if pageUrl does not have a / at the start, add it
        if (!pageUrl.StartsWith("/"))
        {
            pageUrl = "/" + pageUrl;
        }
        using var response = await _httpClient.Get(pageUrl, cancellationToken: cancellationToken);
        string s;
        using (var sr = new StreamReader(await response.Content.ReadAsStreamAsync(cancellationToken)))
        {
            s = await sr.ReadToEndAsync(cancellationToken);
        }
        
        var context = ContextPage.Parser.ParseJson(s);
        return context;
    }
}