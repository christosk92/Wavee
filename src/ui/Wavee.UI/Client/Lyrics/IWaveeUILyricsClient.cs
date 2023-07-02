using Wavee.Metadata;

namespace Wavee.UI.Client.Lyrics;

public interface IWaveeUILyricsClient
{
    Task<LyricsLine[]> GetLyrics(string trackId, CancellationToken ct);
}

