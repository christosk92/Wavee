using System.Net;
using System.Threading.Channels;
using Wavee.Core.Exceptions;
using Wavee.Spotify.Models.Response;
using Wavee.Spotify.Playback.Cdn;

namespace Wavee.Spotify.Playback;

public abstract class SpotifyEncryptedStream : IDisposable
{
    protected SpotifyEncryptedStream(long totalSize)
    {
        Length = totalSize;
    }

    public long Length { get; }
    public abstract bool IsCached { get; }

    public abstract Task<byte[]> GetChunk(int chunkIndex, CancellationToken cancel);

    public static async Task<SpotifyEncryptedStream> Open(ISpotifyClient spotifyClient, SpotifyTrackInfo track,
        SpotifyAudioFile file)
    {
        if (spotifyClient.Cache.TryGetFile(file, out var fileStream))
        {
            return new CachedSpotifyEncryptedStream(fileStream);
        }

        var cdnurl = new CdnUrl(file.FileId);
        await cdnurl.ResolveAudio(spotifyClient);
        if (!cdnurl.TryGetUrl(out var url))
        {
            throw new CannotPlayException(CannotPlayException.Reason.CdnError);
        }

        // Get the first chunk with the headers to get the file size.
        var streamer = await spotifyClient.Cdn.StreamFromCdnAsync(url, offset: 0, length: SpotifyUrls.Cdn.ChunkSize);
        if (streamer.StatusCode is not HttpStatusCode.PartialContent)
        {
            throw new CannotPlayException(CannotPlayException.Reason.CdnError);
        }

        var contentRange = streamer.Content.Headers.ContentRange;
        var totalSize = contentRange?.Length ?? throw new CannotPlayException(CannotPlayException.Reason.CdnError);

        var chunkRequestChannel = Channel.CreateUnbounded<ChunkRequest>();
        var chunkRequestRx = chunkRequestChannel.Reader;
        var chunkRequestTx = chunkRequestChannel.Writer;
        Console.WriteLine("Starting audiofetching");
        _ = Task.Run(async () => await AudioFileFetch(
            spotifyClient,
            chunkRequestRx,
            streamer
        ));

        return new StreamingFile(
            file: file,
            track: track,
            totalSize: totalSize,
            chunkRequestTx: chunkRequestTx
        );
    }

    public record ChunkRequest(int Index, TaskCompletionSource<byte[]> Tcs);

    private static async Task AudioFileFetch(ISpotifyClient spotifyClient,
        ChannelReader<ChunkRequest> chunkRequestRx,
        HttpResponseMessage initialChunk)
    {
        await foreach (var chunkIndex in chunkRequestRx.ReadAllAsync())
        {
            if (chunkIndex.Index is 0)
            {
                chunkIndex.Tcs.TrySetResult(await initialChunk.Content.ReadAsByteArrayAsync());
                continue;
            }

            var chunk = await GetChunkFromCdn(spotifyClient, chunkIndex.Index, initialChunk);
            chunkIndex.Tcs.TrySetResult(chunk);
        }
    }

    private static async Task<byte[]> GetChunkFromCdn(ISpotifyClient spotifyClient, int chunkIndex,
        HttpResponseMessage initialChunk)
    {
        var offset = chunkIndex * SpotifyUrls.Cdn.ChunkSize;
        var length = SpotifyUrls.Cdn.ChunkSize;
        var streamer =
            await spotifyClient.Cdn.StreamFromCdnAsync(initialChunk.RequestMessage!.RequestUri!.ToString(), offset,
                length);
        return await streamer.Content.ReadAsByteArrayAsync();
    }

    public virtual void Dispose()
    {
        // TODO release managed resources here
    }
}