using System.Collections.Immutable;
using Wavee.Enums;
using Wavee.Interfaces.Models;
using Wavee.Models;

namespace Wavee.UI.Models.Local;

/// <summary>
/// Represents a lightweight album with minimal data, typically derived from a track.
/// Note: This struct does not hold extensive data about an album or its location on disk.
/// Its purpose is to provide a simple representation of an album based on the information
/// available in a related track.
/// </summary>
/// <param name="Image">The optional image URL associated with the album.</param>
/// <param name="Title">The title of the album.</param>
/// <param name="Service">The type of service the album belongs to.</param>
public readonly record struct LocalAlbum(
    string? Image,
    string Title,
    ImmutableArray<DescriptionItem> Artists,
    uint Year,
    uint NumberOfTracks,
    ulong SumDuration,
    DateTime MaxDateAdded,
    int SumPlayCount,
    DateTime? MaxLastPlayed,
    ServiceType Service) : IAlbum
{

    public string Id => Title;

    public bool Equals(IAudioItem? other)
    {
        return Id == other?.Id && Service == other.Service;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, (int)Service);
    }
}