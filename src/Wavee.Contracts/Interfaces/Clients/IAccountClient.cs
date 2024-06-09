namespace Wavee.Contracts.Interfaces.Clients;

public interface IAccountClient
{
    IHomeClient Home { get; }
    IColorClient Color { get; }
    IPlaybackClient Playback { get; }
}