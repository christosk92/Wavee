using Wavee.Models.Common;
using Wavee.Models.Metadata;

namespace Wavee.ViewModels.Models.Items;

public record WaveeSongItem(
    SpotifyId Id,
    string Title,
    TimeSpan Duration,
    WaveeSongAlbum Album,
    IEnumerable<WaveeSongArtist> Artists,
    Dictionary<SpotifyImageSize, SpotifyImage> Images,
    bool CanPlay) : WaveeItem(Id, Title);