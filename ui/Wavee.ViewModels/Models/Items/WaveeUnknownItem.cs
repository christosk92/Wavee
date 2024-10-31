using Wavee.Models.Common;

namespace Wavee.ViewModels.Models.Items;

public sealed record WaveeUnknownItem(SpotifyId Id) : WaveeItem(Id, string.Empty);