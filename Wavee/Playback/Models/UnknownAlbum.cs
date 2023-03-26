using System.Collections.Immutable;
using System.Security.Cryptography;
using Wavee.Enums;
using Wavee.Interfaces.Models;
using Wavee.Models;

namespace Wavee.UI.Models.Local;

internal readonly record struct UnknownAlbum() : IAlbum
{
    internal static readonly string __real_unknown_album;

    public bool Equals(IAudioItem? other)
    {
        return other?.Id == __real_unknown_album;
    }

    public string? Image => null;
    public string Title => "Unknown Album";
    public string Id => __real_unknown_album;
    public ServiceType Service => ServiceType.Local;
    static UnknownAlbum()
    {
        __real_unknown_album = RandomNumberGenerator.GetHexString(24, true);
    }

    public ImmutableArray<DescriptionItem> Artists => ImmutableArray<DescriptionItem>.Empty;
    public uint Year
    {
        get;
    }

    public uint NumberOfTracks
    {
        get;
    }
}