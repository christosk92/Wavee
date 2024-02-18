using AsyncKeyedLock;
using Google.Protobuf;
using Wavee.Core.Extensions;
using Wavee.Spotify.Authenticators;
using Wavee.Spotify.Http.Interfaces;
using Wavee.Spotify.Models.Common;

namespace Wavee.Spotify.Auth;

public abstract class SpotifyTcpAuthenticator : IAuthenticator
{
    private readonly AsyncNonKeyedLocker _locker = new();
    protected SpotifyInternalAuthClient? AuthClient;
    public BearerTokenResponse? Token { get; private set; }

    public async Task Apply(string deviceId, IRequest request, IAPIConnector apiConnector)
    {
        //Ensure.ArgumentNotNull(request, nameof(request));
        Guard.NotNull(nameof(request), request);
        Token = await GetTokenVal(deviceId, apiConnector, CancellationToken.None).ConfigureAwait(false);

        request.Headers["Authorization"] = $"{Token.TokenType} {Token.AccessToken}";
    }

    public Task<string> GetToken(string deviceId, IAPIConnector apiConnector, CancellationToken cancel = default)
    {
        return GetTokenVal(deviceId, apiConnector, cancel).ContinueWith(x => x.Result.AccessToken, cancel);
    }

    Task<byte[]?> IAuthenticator.GetAudioKey(SpotifyId id, ByteString fileId, CancellationToken cancellationToken)
        => AuthClient.RequestAudioKey(id, fileId, cancellationToken);

    public async Task<BearerTokenResponse> GetTokenVal(string deviceId, IAPIConnector apiConnector,
        CancellationToken cancel = default)
    {
        if (AuthClient == null)
        {
            throw new InvalidOperationException("AuthClient is not initialized");
        }

        using var locker = await _locker.LockAsync(cancel);
        if (Token == null || Token.IsExpired)
        {
            Console.WriteLine("Token is null or expired");
            var token = await AuthClient!.RequestToken(deviceId, apiConnector, CancellationToken.None)
                .ConfigureAwait(false);
            Token = token;
            return token!;
        }

        Console.WriteLine("Token is not null or expired");
        return Token;
    }
}