using System.Security.Cryptography;
using Wavee.Enums;
using Wavee.Interfaces.Models;

namespace Wavee.UI.Models.Local;

internal readonly record struct UnknownArtist() : IAlbum
{
    internal static readonly string __real_unknown_artist;

    public bool Equals(IAudioItem? other)
    {
        return other?.Id == __real_unknown_artist;
    }

    public string? Image => null;
    public string Title => "Unknown Artist";
    public string Id => __real_unknown_artist;
    public ServiceType Service => ServiceType.Local;
    static UnknownArtist()
    {
        __real_unknown_artist = RandomNumberGenerator.GetHexString(24, true);
    }
}