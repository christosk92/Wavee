using Wavee.Models.Common;

namespace Wavee.ViewModels.Models.Items;

public record WaveeSongAlbum(SpotifyId Id, string Title) : WaveeItem(Id, Title);