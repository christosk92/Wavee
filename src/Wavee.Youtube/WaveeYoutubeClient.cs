using Microsoft.VisualBasic;
using System.Collections.Generic;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Search;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace Wavee.Youtube;

internal sealed class WaveeYoutubeClient : IWaveeYoutubeClient
{
    private readonly YoutubeClient _youtubeClient;

    public WaveeYoutubeClient(IHttpClientFactory httpClientFactory)
    {
        _youtubeClient = new YoutubeClient(httpClientFactory.CreateClient(Constants.YoutubeClientName));
    }

    // Search
    public IAsyncEnumerable<VideoSearchResult> SearchAsync(string query,
        CancellationToken cancellationToken)
    {
        IAsyncEnumerable<VideoSearchResult> searchResult = _youtubeClient.Search.GetVideosAsync(query, cancellationToken);

        return searchResult;
    }

    // Get stream
    public async Task<Stream> GetStreamAsync(string videoId, CancellationToken cancellationToken)
    {
        var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(videoId, cancellationToken);

        var muxed = streamManifest.GetMuxedStreams()
            .GetWithHighestBitrate();

        var stream = await _youtubeClient.Videos.Streams.GetAsync(muxed, cancellationToken);
        return stream;
    }

    public ValueTask<Video> GetVideoAsync(string videoId, CancellationToken cancellationToken)
    {
        var video = _youtubeClient.Videos.GetAsync(videoId, cancellationToken);
        return video;
    }
}

public interface IWaveeYoutubeClient
{
    IAsyncEnumerable<VideoSearchResult> SearchAsync(string query, CancellationToken cancellationToken);
    Task<Stream> GetStreamAsync(string videoId, CancellationToken cancellationToken);

    ValueTask<Video> GetVideoAsync(string videoId, CancellationToken cancellationToken);
}