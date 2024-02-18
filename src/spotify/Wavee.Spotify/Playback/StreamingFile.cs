using System.Threading.Channels;
using Wavee.Spotify.Models.Interfaces;
using Wavee.Spotify.Models.Response;

namespace Wavee.Spotify.Playback;

public sealed class StreamingFile : SpotifyEncryptedStream
{
    private readonly ChannelWriter<ChunkRequest> _chunkRequestTx;
    private readonly ISpotifyPlayableItem _track;
    private readonly SpotifyAudioFile _file;

    public StreamingFile(SpotifyAudioFile file,
        ISpotifyPlayableItem track,
        long totalSize,
        ChannelWriter<ChunkRequest> chunkRequestTx) : base(totalSize)
    {
        _file = file;
        _track = track;
        _chunkRequestTx = chunkRequestTx;
    }

    public override bool IsCached => false;

    public override async Task<byte[]> GetChunk(int chunkIndex, CancellationToken cancel)
    {
        var tcs = new TaskCompletionSource<byte[]>();
        var request = new ChunkRequest(chunkIndex, tcs);
        await _chunkRequestTx.WriteAsync(request, cancel);
        return await tcs.Task.WaitAsync(cancel);
    }
}