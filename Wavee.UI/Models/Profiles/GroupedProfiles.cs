using System.Collections.Immutable;

namespace Wavee.UI.Models.Profiles;

public readonly record struct GroupedProfiles(ImmutableArray<Profile> Profiles, string Key);