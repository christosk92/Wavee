using System.Net.Http.Headers;
using System.Text.Json;
using Eum.Spotify;
using Eum.Spotify.context;
using Eum.Spotify.login5v3;
using Eum.Spotify.storage;
using Google.Protobuf;
using Microsoft.VisualBasic;
using Wavee.Spfy.Playback;
using Wavee.Spfy.Playback.Decrypt;
using ClientInfo = Eum.Spotify.login5v3.ClientInfo;

namespace Wavee.Spfy.DefaultServices;

internal sealed class DefaultHttpClient : IHttpClient
{
    private readonly HttpClient _httpClient = new HttpClient();

    public async Task<SpotifyTokenResult> SendLoginRequest(Dictionary<string, string> body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
        request.Content = new FormUrlEncodedContent(body);
        using var response = await _httpClient.SendAsync(request, ct);
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var jsondoc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var accessToken = jsondoc.RootElement.GetProperty("access_token").GetString();
        var expiresIn = jsondoc.RootElement.GetProperty("expires_in").GetInt32();
        jsondoc.RootElement.GetProperty("refresh_token").GetString();
        jsondoc.RootElement.GetProperty("scope").GetString();
        jsondoc.RootElement.GetProperty("token_type").GetString();
        var finalUsername = jsondoc.RootElement.GetProperty("username").GetString();

        return new SpotifyTokenResult(accessToken, DateTimeOffset.Now.AddSeconds(expiresIn), finalUsername);
    }

    public async Task<(string ap, string dealer, string sp)> FetchBestAccessPoints()
    {
        const string url = "https://apresolve.spotify.com/?type=accesspoint&type=dealer&type=spclient";
        await using var stream = await _httpClient.GetStreamAsync(url);
        using var jsondoc = await JsonDocument.ParseAsync(stream);
        var ap = jsondoc.RootElement.GetProperty("accesspoint").EnumerateArray().First().GetString();
        var dealer = jsondoc.RootElement.GetProperty("dealer").EnumerateArray().First().GetString();
        var sp = jsondoc.RootElement.GetProperty("spclient").EnumerateArray().First().GetString();
        return (ap, dealer, sp);
    }

    public async Task<LoginResponse> SendLoginStepTwoRequest(LoginCredentials credentials, string deviceId,
        CancellationToken cancellation)
    {
        var loginRequest = new LoginRequest
        {
            ClientInfo = new ClientInfo
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
        var loginRequestBytes = loginRequest.ToByteArray();
        using var httpLoginRequest = new HttpRequestMessage(HttpMethod.Post, "https://login5.spotify.com/v3/login");
        httpLoginRequest.Headers.Add("User-Agent", "Spotify/122400756 Win32_x86_64/0 (PC laptop)");
        using var byteArrayContent = new ByteArrayContent(loginRequestBytes);
        byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");
        httpLoginRequest.Content = byteArrayContent;
        using var loginResponse = await _httpClient.SendAsync(httpLoginRequest, cancellation);
        await using var loginStream = await loginResponse.Content.ReadAsStreamAsync(cancellation);
        var loginResponseFinal = LoginResponse.Parser.ParseFrom(loginStream);
        return loginResponseFinal;
    }

    public Task<HttpResponseMessage> Get(string endpointWithId, string accessToken, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, endpointWithId);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return _httpClient.SendAsync(request, cancellationToken);
    }

    public Task<HttpResponseMessage> Get(string url)
    {
        return _httpClient.GetAsync(url);
    }

    public async Task<StorageResolveResponse> StorageResolve(string fileFileIdBase16, string accessToken,
        CancellationToken cancellationToken = default)
    {
        var spclient = await ApResolve.GetSpClient(this);
        var url = $"https://{spclient}/storage-resolve/files/audio/interactive/{fileFileIdBase16}";
        return await Get(url, accessToken, cancellationToken).ContinueWith(x =>
            {
                x.Result.EnsureSuccessStatusCode();
                return x.Result.Content.ReadAsStreamAsync(cancellationToken);
            }, cancellationToken).Unwrap()
            .ContinueWith(x => StorageResolveResponse.Parser.ParseFrom(x.Result), cancellationToken);
    }

    public async Task<SpotifyEncryptedStream> CreateEncryptedStream(string cdnUrl,
        CancellationToken cancellationToken = default)
    {
        // get the first chunk
        var chunkSize = SpotifyDecryptedStream.ChunkSize;
        var start = 0;
        var end = chunkSize - 1;
        var (firstChunk, totalSize) = await GetChunk(cdnUrl, start, end, cancellationToken);
        return new SpotifyEncryptedStream(firstChunk, totalSize, (i) =>
        {
            var start2 = i * chunkSize;
            var end2 = start2 + chunkSize - 1;
            return GetChunk(cdnUrl, start2, end2, cancellationToken);
        });
    }

    public async Task<Context> ResolveContext(string itemId, string accessToken)
    {
        var url = $"/context-resolve/v1/{itemId.ToString()}";
        var spclient = await ApResolve.GetSpClient(this);
        var finalUrl = $"https://{spclient}{url}";
        using var request = new HttpRequestMessage(HttpMethod.Get, finalUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/protobuf"));
        using var response = await _httpClient.SendAsync(request);
        var stream = await response.Content.ReadAsStringAsync();
        var context = Context.Parser.ParseJson(stream);
        return context;
    }

    public async Task<ContextPage> ResolveContextRaw(string pageUrl, string accessToken)
    {
        if (!pageUrl.StartsWith("/"))
        {
            pageUrl = "/" + pageUrl;
        }

        var spclient = await ApResolve.GetSpClient(this);
        var finalUrl = $"https://{spclient}{pageUrl}";
        using var request = new HttpRequestMessage(HttpMethod.Get, finalUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await _httpClient.SendAsync(request);
        string s;
        using (var sr = new StreamReader(await response.Content.ReadAsStreamAsync(CancellationToken.None)))
        {
            s = await sr.ReadToEndAsync(CancellationToken.None);
        }

        try
        {
            var context = ContextPage.Parser.ParseJson(s);
            return context;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private async Task<(byte[] bytes, long totalSize)> GetChunk(string cdnUrl, long start, long end,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, cdnUrl);
        request.Headers.Range = new RangeHeaderValue(start, end);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var totalSize = response.Content.Headers.ContentRange!.Length!.Value;
        return (bytes, totalSize);
    }
}