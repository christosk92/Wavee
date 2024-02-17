using System.Net.Http.Headers;
using Eum.Spotify;
using Eum.Spotify.login5v3;
using Google.Protobuf;
using Wavee.Spotify.Auth.OAuth;
using Wavee.Spotify.Auth.Tcp;
using Wavee.Spotify.Authenticators;
using Wavee.Spotify.Http.Interfaces;
using ClientInfo = Eum.Spotify.ClientInfo;

namespace Wavee.Spotify.Auth;

public sealed class SpotifyInternalAuthClient : ISpotifyInternalAuthClient
{
    private TcpAuth? _tcpAuth;
    private readonly OpenBrowserRequest? _openBrowser;
    private LoginCredentials? _credentials;

    /// <summary>
    /// Use this constructor if you want to use the internal auth client to authenticate with Spotify with pre-existing credentials.
    /// </summary>
    /// <param name="initialCredentials"></param>
    public SpotifyInternalAuthClient(LoginCredentials? initialCredentials)
    {
        _openBrowser = null;
        _credentials = initialCredentials;
    }

    /// <summary>
    /// Use this constructor if you want to use the internal auth client to authenticate with Spotify without pre-existing credentials.
    ///
    /// This will open a browser window and prompt the user to login to Spotify and authorize the application.
    /// </summary>
    public SpotifyInternalAuthClient(OpenBrowserRequest openBrowser)
    {
        _openBrowser = openBrowser;
        _credentials = null;
    }

    public async Task<BearerTokenResponse?> RequestToken(string deviceId, IAPIConnector apiConnector,
        CancellationToken cancellationToken)
    {
        if (_credentials is null)
        {
            //If the credentials are null, we need to authenticate the user
            var tokenCredentials = await OAuthFlow.GetToken(apiConnector, _openBrowser, cancellationToken);
            _tcpAuth?.Dispose();
            _tcpAuth = new TcpAuth(tokenCredentials, deviceId);
            _credentials = await _tcpAuth.Authenticate();
        }
        else if (_credentials.Typ is AuthenticationType.AuthenticationUserPass)
        {
            //Probably re-usable credentials
            _tcpAuth?.Dispose();
            _tcpAuth = new TcpAuth(_credentials, deviceId);
            _credentials = await _tcpAuth.Authenticate();
        }

        var countryCode = await _tcpAuth!.CountryCode;

        return await FinalStep(_credentials.Username, deviceId, _credentials, apiConnector, cancellationToken);
    }

    private async Task<BearerTokenResponse> FinalStep(
        string username,
        string deviceId, LoginCredentials credentials,
        IAPIConnector apiConnector, CancellationToken cancel)
    {
        var loginRequest = new LoginRequest
        {
            ClientInfo = new Eum.Spotify.login5v3.ClientInfo
            {
                ClientId = Constants.SpotifyClientId,
                DeviceId = deviceId,
            },
            StoredCredential = new StoredCredential
            {
                Data = credentials.AuthData,
                Username = credentials.Username
            }
        };

        var byteArrayContent = new ByteArrayContent(loginRequest.ToByteArray());
        byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");
        var loginResponse = await apiConnector.Post<LoginResponse>(new Uri(SpotifyUrls.Login.LoginV3), null,
            byteArrayContent,
            RequestContentType.Protobuf,
            null, cancel);
        if (loginResponse.Error is not LoginError.UnknownError)
        {
            throw new AuthException(loginResponse.Error.ToString());
        }

        var token = new BearerTokenResponse
        {
            AccessToken = loginResponse.Ok.AccessToken,
            ExpiresIn = loginResponse.Ok.AccessTokenExpiresIn,
            Scope = Constants.Scopes,
            TokenType = "Bearer"
        };
        _credentials = new LoginCredentials
        {
            Username = username,
            AuthData = loginResponse.Ok.StoredCredential,
            Typ = AuthenticationType.AuthenticationStoredSpotifyCredentials
        };

        return token;
    }
}

public interface ISpotifyInternalAuthClient
{
}