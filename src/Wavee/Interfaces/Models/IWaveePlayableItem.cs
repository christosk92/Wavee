namespace Wavee.Interfaces.Models;

public interface IWaveePlayableItem
{
    TimeSpan Duration { get; }
    string? Id { get; }
}