namespace Wavee.Player.Commanding;

public readonly record struct InternalPlayCommand<RT>(
    RT Runtime,
    string PlaybackId,
    IAudioStream Stream) : IInternalPlayerCommand;

public readonly record struct InternalPauseCommand<RT>(
    RT Runtime) : IInternalPlayerCommand;

public readonly record struct InternalSeekCommand<RT>(RT runtime,
    TimeSpan To
) : IInternalPlayerCommand;