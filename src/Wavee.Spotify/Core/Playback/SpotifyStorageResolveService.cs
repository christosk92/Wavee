using Eum.Spotify.storage;
using Wavee.Spotify.Core.Models.Track;
using Wavee.Spotify.Infrastructure.HttpClients;
using Wavee.Spotify.Infrastructure.Playback;
using Wavee.Spotify.Interfaces;
using Wavee.Spotify.Interfaces.Clients.Playback;

namespace Wavee.Spotify.Core.Clients.Playback;

internal sealed class SpotifyStorageResolveService : ISpotifyStorageResolveService
{
    private readonly ISpotifyTokenService _tokenService;
    private readonly SpotifyInternalHttpClient _httpClient;
    private readonly AudioStreamingHttpClient _audioStreamingHttpClient;

    public SpotifyStorageResolveService(
        ISpotifyTokenService tokenService,
        SpotifyInternalHttpClient httpClient,
        AudioStreamingHttpClient audioStreamingHttpClient)
    {
        _tokenService = tokenService;
        _httpClient = httpClient;
        _audioStreamingHttpClient = audioStreamingHttpClient;
    }


    public async Task<SpotifyStreamingFile> GetStorageFile(SpotifyAudioFile file, CancellationToken cancellationToken)
    {
        var url = $"/storage-resolve/files/audio/interactive/{file.FileIdBase16}";
        using var response = await _httpClient.Get(url, "application/protobuf", cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var res = StorageResolveResponse.Parser.ParseFrom(stream);
        var cdnUrl = res.Cdnurl.First();

        // Read first chunk
        var (firstChunk, totalSize) =
            await SpotifyStreamingFile.GetChunk(res.Cdnurl.First(), _audioStreamingHttpClient, 0, cancellationToken);
        return new SpotifyStreamingFile(
            totalSize: totalSize,
            cdnUrl: cdnUrl,
            firstChunk: firstChunk,
            file: file,
            httpClient: _audioStreamingHttpClient
        );
    }
}