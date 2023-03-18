using Wavee.Enums;
using Wavee.Interfaces.Models;

namespace Wavee.UI.Models.Local;

/// <summary>
/// Represents a lightweight artist with minimal data.
/// Note: This struct does not hold extensive data about an artist.
/// Its purpose is to provide a simple representation of an artist based on available information of a track.
/// </summary>
/// <param name="Image">The optional image URL associated with the artist.</param>
/// <param name="Title">The name of the artist.</param>
/// <param name="Service">The type of service the artist belongs to.</param>
public readonly record struct LocalArtist(
        string? Image,
        string Title,
        ServiceType Service) : IArtist
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