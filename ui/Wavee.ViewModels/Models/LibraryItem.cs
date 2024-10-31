using Wavee.Models.Common;
using Wavee.ViewModels.Enums;

namespace Wavee.ViewModels.Models;

public sealed record LibraryItem(SpotifyId Id, LibraryItemType Type, DateTimeOffset AddedAt, WaveeItem Item);