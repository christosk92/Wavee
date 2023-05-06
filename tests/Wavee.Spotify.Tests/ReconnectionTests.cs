using System.Net;
using System.Net.Http.Headers;
using System.Threading.Channels;
using Eum.Spotify;
using Google.Protobuf;
using LanguageExt;
using Moq;
using Wavee.Infrastructure.Traits;
using Wavee.Spotify.Infrastructure.Connection;
using Wavee.Spotify.Infrastructure.Sys;

namespace Wavee.Spotify.Tests;

public class ReconnectionTests
{
    [Fact]
    public async Task Reconnects_After_Connection_Failure()
    {
        // Arrange
        var apResolveUrl = "https://apresolve.spotify.com/?type=accesspoint&type=dealer&type=spclient";
        var credentials = new LoginCredentials
        {
            Username = "test",
            Typ = AuthenticationType.AuthenticationUserPass,
            AuthData = ByteString.CopyFromUtf8("test")
        };

        var connectionId = Guid.NewGuid();
        var deviceId = Guid.NewGuid().ToString();

        // Create a test runtime with a custom HttpIO implementation that simulates connection failure and reconnection
        var testHttpIO = new TestHttpIO();
        var testRuntime = TestRuntimeC.New(testHttpIO);

        var client = new Mock<ISpotifyClient>();
        var reader = Channel.CreateUnbounded<SpotifyPacket>();

        // Act
        await SpotifyRuntime.Reconnect(client.Object, reader.Reader, credentials,
            connectionId,
            deviceId, CancellationToken.None, testRuntime);

        // Assert
        client.Verify(c => ((SpotifyClient<TestRuntimeC>)c).OnApWelcome(It.IsAny<APWelcome>()), Times.Once);
    }

    // Custom HttpIO implementation to simulate connection failure and reconnection
}

internal class TestHttpIO : HttpIO
{
    private int _connectionAttempts = 0;

    public ValueTask<HttpResponseMessage> Get(string url, Option<AuthenticationHeaderValue> authentication,
        Option<HashMap<string, string>> headers, CancellationToken ct = default)
    {
        _connectionAttempts++;

        if (_connectionAttempts == 1)
        {
            // Simulate connection failure on the first attempt
            throw new HttpRequestException("Connection failure");
        }

        // Simulate successful connection on the second attempt
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StringContent("<APWelcome>"); // Replace with the actual APWelcome content
        return ValueTask.FromResult(response);
    }

    public ValueTask<HttpResponseMessage> GetWithContentRange(string url, int start, int length, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<HttpResponseMessage> Put(string url, Option<AuthenticationHeaderValue> authheader, Option<HashMap<string, string>> headers, HttpContent content, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}

internal readonly struct TestRuntimeC : HasHttp<TestRuntimeC>, HasTCP<TestRuntimeC>, HasWebsocket<TestRuntimeC>
{
    private readonly HttpIO httpIo;

    TestRuntimeC(HttpIO httpIo)
    {
        this.httpIo = httpIo;
        HttpEff = Eff<TestRuntimeC, HttpIO>.Success(httpIo);
    }

    public static TestRuntimeC New(HttpIO f) =>
        new TestRuntimeC(f);

    public Eff<TestRuntimeC, HttpIO> HttpEff { get; }


    public CancellationToken Cancel => throw new NotImplementedException();

    public TestRuntimeC LocalCancel =>
        new TestRuntimeC(httpIo);

    public CancellationToken CancellationToken => CancellationTokenSource.Token;
    public CancellationTokenSource CancellationTokenSource { get; } = new();
    public Eff<TestRuntimeC, TcpIO> TcpEff { get; }
    public Eff<TestRuntimeC, WebsocketIO> WsEff { get; }
}