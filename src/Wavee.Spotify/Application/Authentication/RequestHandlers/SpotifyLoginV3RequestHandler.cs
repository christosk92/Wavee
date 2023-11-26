using System.Net.Http.Headers;
using System.Reflection.Metadata;
using Eum.Spotify.login5v3;
using Google.Protobuf;
using Mediator;
using Wavee.Spotify.Application.Authentication.Requests;

namespace Wavee.Spotify.Application.Authentication.RequestHandlers;

public sealed class SpotifyLoginV3RequestHandler : IRequestHandler<SpotifyLoginV3Request, LoginResponse>
{
    private readonly HttpClient _accountsApi;

    public SpotifyLoginV3RequestHandler(IHttpClientFactory accountsApi)
    {
        _accountsApi = accountsApi.CreateClient(Constants.SpotifyAccountsApiHttpClient);
    }

    public async ValueTask<LoginResponse> Handle(SpotifyLoginV3Request request, CancellationToken cancellationToken)
    {
        //Next we login https://login5.spotify.com/v3/login
        //Content-Type: application/x-protobuf
        //User-Agent: Spotify/122400756 Win32_x86_64/0 (PC laptop)
        var loginRequest = new LoginRequest
        {
            ClientInfo = new ClientInfo
            {
                ClientId = Constants.SpotifyClientId,
                DeviceId = request.DeviceId,
            },
            StoredCredential = request.Request
        };
        var loginRequestBytes = loginRequest.ToByteArray();
        using var httpLoginRequest = new HttpRequestMessage(HttpMethod.Post, "https://login5.spotify.com/v3/login");
        httpLoginRequest.Headers.Add("User-Agent", "Spotify/122400756 Win32_x86_64/0 (PC laptop)");
        using var byteArrayContent = new ByteArrayContent(loginRequestBytes);
        byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");
        httpLoginRequest.Content = byteArrayContent;
        using var loginResponse = await _accountsApi.SendAsync(httpLoginRequest, cancellationToken);
        await using var loginStream = await loginResponse.Content.ReadAsStreamAsync(cancellationToken);
        var loginResponseFinal = LoginResponse.Parser.ParseFrom(loginStream);
        return loginResponseFinal;
    }
}