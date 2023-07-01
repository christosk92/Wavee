using Wavee.Metadata;

namespace Wavee.UI.Client.Lyrics;

public interface IWaveeUILyricsClient
{
    ValueTask<LyricsLine[]> GetLyrics(string trackId, CancellationToken ct);
}

