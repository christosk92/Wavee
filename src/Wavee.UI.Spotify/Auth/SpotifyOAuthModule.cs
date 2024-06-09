using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Eum.Spotify;
using Google.Protobuf;
using Microsoft.VisualBasic;
using Refit;
using Wavee.UI.Spotify.ContentSerializers;
using Wavee.UI.Spotify.Interfaces;
using Wavee.UI.Spotify.Responses;

namespace Wavee.UI.Spotify.Auth;

internal sealed class SpotifyOAuthModule : ISpotifyAuthModule
{
    private readonly SpotifyOAuthConfiguration _oauth;
    private readonly string _deviceId;

    public SpotifyOAuthModule(SpotifyOAuthConfiguration oauth, string deviceId)
    {
        _oauth = oauth;
        _deviceId = deviceId;
        var refitSettings = new RefitSettings
        {
            ContentSerializer = new ProtobufContentSerializer()
        };
        var jsonRefitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            })
        };
        AccountClient = RestService.For<ISpotifyAccountClient>(SpotifyUrls.Account.BaseUrl, jsonRefitSettings);
        LoginClient = RestService.For<ISpotifyLoginClient>(SpotifyUrls.Login.BaseUrl, refitSettings);
    }

    public ISpotifyAccountClient AccountClient { get; }
    public ISpotifyLoginClient LoginClient { get; }

    public async ValueTask<ISpotifyConnection> Login(CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromMinutes(1));
        try
        {
            var stored = await _oauth.Storage.Get();
            if (stored is not null)
            {
                var connection = await SpotifyTcpConnection.Create(LoginCredentials.Parser.ParseFrom(stored), _deviceId,
                    cancellationToken);
                if (connection.IsConnected)
                {
                    return connection;
                }

                connection.Dispose();
            }
        }
        catch (Exception x)
        {
            Console.WriteLine(x);
        }


        var newUrl = OAuthUtils.CreateNewUrl(out string redirect, out string codeVerifier);
        using var listener = new OAuthFlow(new Uri(redirect));
        await _oauth.OpenBrowserRequest(newUrl);
        _ = Task.Run(async () => await listener!.StartListener(), cts.Token);
        var result = await listener.TokenTask.WaitAsync(cts.Token);
        if (!string.IsNullOrEmpty(result))
        {
            return await FinalStep(result, redirect, codeVerifier, cancellationToken);
        }

        return null;
    }

    private async Task<ISpotifyConnection> FinalStep(string result, string redirect, string codeVerifier,
        CancellationToken cancellationToken)
    {
        var code = OAuthUtils.MatchRegex().Match(result).Groups[1].Value;
        var form = new Dictionary<string, string>
        {
            ["client_id"] = SpotifyConstants.SpotifyClientId,
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirect,
            ["code_verifier"] = codeVerifier
        };


        var credentials = await AccountClient.GetCredentials(form, cancellationToken);
        var loginCredentials = new LoginCredentials
        {
            Username = credentials.Username,
            AuthData = ByteString.CopyFromUtf8(credentials.AccessToken),
            Typ = AuthenticationType.AuthenticationSpotifyToken
        };
        var connection = await SpotifyTcpConnection.Create(loginCredentials, _deviceId, cancellationToken);
        await _oauth.Storage.Store(connection.AuthenticatedCredentials.ToByteArray());
        return connection;
    }
}