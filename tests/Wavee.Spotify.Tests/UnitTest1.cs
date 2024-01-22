using Eum.Spotify;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace Wavee.Spotify.Tests;

public class UnitTest1
{
    [Fact]
    public void InitializeTestShouldOk()
    {
        Client.Initialize(TcpFactory, HttpFactory);

        var httpClient = Services.HttpClient;
        var tcpClient = Services.TcpClientFactory();

        Assert.Equal(typeof(FakeHttpClient), httpClient.GetType());
        Assert.Equal(typeof(FakeTcpClient), tcpClient.GetType());
        return;

        ITcpClient TcpFactory()
        {
            return new FakeTcpClient();
        }

        IHttpClient HttpFactory()
        {
            return new FakeHttpClient();
        }
    }


    [Fact]
    public async Task OpenBrowserRedirectTo1270015001()
    {
        Client.Initialize(TcpFactory, HttpFactory);

        try
        {
            var connection = await Client.CreateConnection(OpenBrowser, new MemorySecureStorage());
        }
        catch (Exception)
        {
            Assert.Fail("Exception occurred");
        }

        return;

        ValueTask<string> OpenBrowser(string url, Func<string, bool> shouldreturn)
        {
            var urlToReturnb = "http://127.0.0.1:5001/login";
            var shouldReturn = shouldreturn(urlToReturnb);
            if (!shouldReturn)
            {
                throw new TestCanceledException("Returned false when should be true");
            }

            return new ValueTask<string>(urlToReturnb);
        }


        ITcpClient TcpFactory()
        {
            return new FakeTcpClient();
        }

        IHttpClient HttpFactory()
        {
            return new FakeHttpClient();
        }
    }
}

internal sealed class MemorySecureStorage : ISecureStorage
{
    public ValueTask Store(string username, string pwd)
    {
        throw new NotImplementedException();
    }
}

internal sealed class FakeTcpClient : ITcpClient
{
    public bool IsConnected { get; }
    public event EventHandler<(Exception Error, bool Manual)>? Disconnected;

    public ValueTask Connect(string host, ushort port)
    {
        return ValueTask.CompletedTask;
    }

    public (ReadOnlyMemory<byte> ReceiveKey, ReadOnlyMemory<byte> SendKey) Handshake()
    {
        throw new NotImplementedException();
    }

    public APWelcome Authenticate(LoginCredentials credentials)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        // TODO release managed resources here
    }
}

internal sealed class FakeHttpClient : IHttpClient
{
    public Task<SpotifyTokenResult> SendLoginRequest(Dictionary<string, string> body, CancellationToken none)
    {
        throw new NotImplementedException();
    }

    public Task<(string ap, string dealer, string sp)> FetchBestAccessPoints()
    {
        throw new NotImplementedException();
    }
}