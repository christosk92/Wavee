using System.Buffers.Binary;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Eum.Spotify;
using Google.Protobuf;
using NeoSmart.AsyncLock;
using Serilog;
using Wavee.ContextResolve;
using Wavee.Infrastructure.Connection;
using Wavee.Infrastructure.Mercury;
using Wavee.Spotify.Infrastructure.Connection;

namespace Wavee.Token.Live;

internal readonly struct LiveTokenClient : ITokenClient, IMercuryClient
{
    private readonly record struct AccessToken(string Token, DateTimeOffset ExpiresAt);

    private static Dictionary<Guid, AccessToken> _tokens = new();
    private static readonly AsyncLock AccessTokenLock = new();
    private readonly Guid _connId;

    public LiveTokenClient(Guid connId)
    {
        _connId = connId;
    }

    public async Task<MercuryResponse> Get(string uri, CancellationToken ct = default)
    {
        var response = await MercuryParsers.GetAsync(_connId, uri, ct);
        return response;
    }
    

    public async ValueTask<string> GetToken(CancellationToken ct = default)
    {
        using (await AccessTokenLock.LockAsync(ct))
        {
            while (true)
            {
                try
                {
                    if (_tokens.TryGetValue(_connId, out var f))
                    {
                        if (f.ExpiresAt > DateTimeOffset.UtcNow)
                            return f.Token;
                    }


                    const string KEYMASTER_URI =
                        "hm://keymaster/token/authenticated?scope=user-read-private,user-read-email,playlist-modify-public,ugc-image-upload,playlist-read-private,playlist-read-collaborative,playlist-read&client_id=65b708073fc0480ea92a077233ca87bd&device_id=";

                    var finalData = await MercuryParsers.GetAsync(_connId, KEYMASTER_URI, ct);

                    var tokenData = JsonSerializer.Deserialize<MercuryTokenData>(finalData.Payload.Span);
                    var newToken = new AccessToken(tokenData.AccessToken, DateTimeOffset.UtcNow.AddSeconds(tokenData.ExpiresIn).Subtract(TimeSpan.FromMinutes(1)));
                    _tokens[_connId] = newToken;
                    return tokenData.AccessToken;
                }
                catch (OperationCanceledException canceled)
                {
                    if (canceled.CancellationToken == ct)
                        throw;
                }
                catch (Exception)
                {
                    Debug.WriteLine("Failed to get token, retrying...");
                    await Task.Delay(3000, ct);
                }
            }
        }
    }
    
    internal readonly record struct MercuryTokenData(
        [property: JsonPropertyName("accessToken")]
        string AccessToken,
        [property: JsonPropertyName("expiresIn")]
        ulong ExpiresIn,
        [property: JsonPropertyName("tokenType")]
        string TokenType,
        [property: JsonPropertyName("scope")] string[] Scope,
        [property: JsonPropertyName("permissions")]
        ushort[] Permissions);
}

public interface IMercuryClient
{
    Task<MercuryResponse> Get(string uri, CancellationToken ct = default);
}

public readonly record struct MercuryResponse(Header Header, ReadOnlyMemory<byte> Payload);