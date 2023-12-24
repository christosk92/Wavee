using Wavee.Interfaces.Models;
using Wavee.Spotify.Core.Models.Track;
using Wavee.Spotify.Infrastructure.HttpClients;
using Wavee.Spotify.Interfaces.Clients.Playback;

namespace Wavee.Spotify.Infrastructure.Playback;

internal sealed class SpotifyOfflineStream : SpotifyAudioStream
{
    private readonly Stream _fileStream;

    public SpotifyOfflineStream(IWaveePlayableItem item,
        SpotifyAudioFile file,
        SpotifyAudioKey audioKey,
        bool isOgg,
        Stream fileStream) : base(audioKey, item.Duration)
    {
        _fileStream = fileStream;
    }

    public override long Offset { get; }
    public override long AudioFileSize { get; }

    public override byte[] GetChunk(int chunkIndex, bool preloading, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _fileStream.Dispose();
        }

        base.Dispose(disposing);
    }
}