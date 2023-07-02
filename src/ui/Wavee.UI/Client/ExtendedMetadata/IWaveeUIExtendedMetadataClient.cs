using LanguageExt;
using Wavee.UI.ViewModel.Playlist;

namespace Wavee.UI.Client.ExtendedMetadata;

public interface IWaveeUIExtendedMetadataClient
{
    ValueTask<Dictionary<string, Either<WaveeUIEpisode, WaveeUITrack>>> GetTracks(string[] trackIds, bool returnData,
        CancellationToken ct = default);
}