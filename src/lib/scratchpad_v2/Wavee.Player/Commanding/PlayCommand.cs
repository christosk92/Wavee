namespace Wavee.Player.Commanding;

public readonly record struct InternalPlayCommand<RT>(
    RT Runtime,
    IAudioStream Stream) : IInternalPlayerCommand;

public readonly record struct InternalPauseCommand<RT>(
    RT Runtime) : IInternalPlayerCommand;