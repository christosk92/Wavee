using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Wavee.Core.AudioCore;
using Wavee.Core.Infrastructure.Live;
using Wavee.Core.Infrastructure.Traits;

[assembly: InternalsVisibleTo("Wavee.Spotify.Tests")]
[assembly: InternalsVisibleTo("Wavee.Player.Tests")]
[assembly: InternalsVisibleTo("Wavee.Spotify")]
[assembly: InternalsVisibleTo("Wavee.Player")]
[assembly: InternalsVisibleTo("Wavee.AudioOutput.LibVLC")]
[assembly: InternalsVisibleTo("Wavee.AudioOutput.NAudio")]
namespace Wavee;

public static class WaveeCore
{
    internal static Ref<Option<ILogger>> Logger = Ref(Option<ILogger>.None);
    internal static Ref<Option<AudioOutputIO>> AudioOutput = Ref(Option<AudioOutputIO>.None);
    internal static Ref<Option<DatabaseIO>> Database = Ref(Option<DatabaseIO>.None);

    static WaveeCore()
    {
        Runtime = WaveeRuntime.New(Logger, AudioOutput, Database);

        Logger.OnChange().Subscribe(c => { Runtime.Env.Logger = c.IfNone(NullLogger.Instance); });
        AudioOutput.OnChange().Subscribe(c => { Runtime.Env.AudioOutputIo = c; });
        Database.OnChange().Subscribe(c => { Runtime.Env.Database = c; });
    }

    public static WaveeRuntime Runtime { get; }
}