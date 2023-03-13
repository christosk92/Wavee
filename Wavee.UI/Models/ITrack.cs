namespace Wavee.UI.Models;

public interface ITrack
{
    string Name { get; }
    string Album { get; }
    string[] Artists { get; }
    string ImagePath { get; }
    public double Duration { get; }
}