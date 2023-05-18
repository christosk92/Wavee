namespace Wavee.Core.Player.InternalCommanding;

internal readonly record struct ResumeCommand : IInternalPlayerCommand
{
}

internal readonly record struct SeekCommand(TimeSpan To) : IInternalPlayerCommand;