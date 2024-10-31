using Wavee.Models.Common;

namespace Wavee.ViewModels.Models.Items;

public record WaveeSongArtist(SpotifyId Id, string Title) : WaveeItem(Id, Title);