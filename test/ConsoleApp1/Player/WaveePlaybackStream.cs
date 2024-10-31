using System.Threading.Tasks.Sources;
using Wavee.Interfaces;
using Wavee.Models.Common;
using Wavee.Playback.Player;
using Wavee.Playback.Streaming;

namespace ConsoleApp1.Player;

internal sealed class WaveePlaybackStream : IDisposable
{
    private AudioStream? _openStream;
    private readonly RequestAudioStreamForTrackAsync? _requestAudioStreamForTrack;
    private SpotifyPlayableItem? _spotifyItem;

    public WaveePlaybackStream(WaveePlayerMediaItem mediaItem,
        RequestAudioStreamForTrackAsync? requestAudioStreamForTrack)
    {
        MediaItem = mediaItem;
        _requestAudioStreamForTrack = requestAudioStreamForTrack;
    }

    public WaveePlayerMediaItem MediaItem { get; }

    public SpotifyPlayableItem? SpotifyItem
    {
        get => _spotifyItem ?? _openStream?.Track;
        set => _spotifyItem = value;
    }

    public bool IsAlive => _openStream != null;

    public ValueTask<Stream> Open(CancellationToken cancellationToken)
    {
        if (_openStream != null)
        {
            return new ValueTask<Stream>(_openStream);
        }

        return new ValueTask<Stream>(OpenAsync(cancellationToken));
    }

    private async Task<Stream> OpenAsync(CancellationToken cancellationToken)
    {
        if (_requestAudioStreamForTrack == null)
        {
            throw new InvalidOperationException("RequestAudioStreamForTrackAsync delegate is not set.");
        }

        var waveeStream = await _requestAudioStreamForTrack?.Invoke(MediaItem, cancellationToken)!;
        await waveeStream!.InitializeAsync(cancellationToken);
        _openStream = waveeStream;
        return waveeStream;
    }

    public void Dispose()
    {
        _openStream?.Dispose();
        _openStream = null;
    }
}