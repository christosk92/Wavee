using System.Collections.Immutable;
using Wavee.Enums;

namespace Wavee.UI.Models.Profiles;

public readonly record struct Profile(
    string Id,
    ServiceType ServiceType,
    string DisplayName,
    string? Image,
    Dictionary<string, string> Properties,
    bool IsDefault,
    double SidebarWidth,
    bool SidebarExpanded,
    bool LargeImage,
    HashSet<string> SavedTracks,
    HashSet<string> SavedAlbums);