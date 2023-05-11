namespace Wavee.Player.States;

public interface IWaveePlayerState
{
}

public readonly record struct InvalidState : IWaveePlayerState
{
    public static readonly InvalidState Default = new();
}