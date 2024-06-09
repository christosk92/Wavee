using Wavee.Contracts.Interfaces.Contracts;

namespace Wavee.Contracts.Interfaces;

public interface ITrack : IPlayableItem
{
    ISimpleAlbum Album { get; }
}