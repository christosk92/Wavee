using Wavee.Interfaces.Models;
using Wavee.Spotify.Infrastructure.HttpClients;
using Wavee.Spotify.Interfaces.Clients.Playback;

namespace Wavee.Spotify.Infrastructure.Playback;

internal sealed class SpotifyCdnStream : SpotifyAudioStream
{
    private const int SPOTIFY_OGG_HEADER_SIZE = 0xa7;

    private readonly SpotifyStreamingFile _file;

    public SpotifyCdnStream(IWaveePlayableItem item,
        SpotifyStreamingFile file,
        SpotifyAudioKey audioKey,
        bool isOgg) : base(audioKey, item.Duration, item)
    {
        Offset = isOgg ? SPOTIFY_OGG_HEADER_SIZE : 0;
        AudioFileSize = file.Length;
        Item = item;
        _file = file;
    }

    public IWaveePlayableItem Item { get; }
    public override long Offset { get; }
    public override long AudioFileSize { get; }

    public override byte[] GetChunk(int chunkIndex, bool preloading, CancellationToken cancellationToken)
    {
        var task = _file.GetChunk(chunkIndex, cancellationToken);
        if(task.IsCompletedSuccessfully)
        {
            return task.Result;
        }
        
        return task.AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    protected override void Dispose(bool disposing)
    {
        _file.Dispose();
        base.Dispose(disposing);
    }
}