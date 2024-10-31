using Wavee.Models.Common;

namespace Wavee.Interfaces;

public interface IRevisionableItem
{
    SpotifyId Id { get; }
    int Index { get; set; }
    string Name { get; }
}