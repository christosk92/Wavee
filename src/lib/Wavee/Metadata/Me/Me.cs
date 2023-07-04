using Wavee.Metadata.Common;

namespace Wavee.Metadata.Me;

public sealed class MeUser
{
    public required string DisplayName { get; set; }
    public CoverImage[] Images { get; set; } = null!;
}