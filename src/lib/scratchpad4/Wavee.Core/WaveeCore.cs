using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Wavee.Core.Infrastructure.Live;
using Wavee.Core.Infrastructure.Traits;

[assembly: InternalsVisibleTo("Wavee.Spotify.Tests")]
[assembly: InternalsVisibleTo("Wavee.Spotify")]
[assembly: InternalsVisibleTo("Wavee.Player")]
namespace Wavee;

internal static class WaveeCore
{
    public static Ref<Option<ILogger>> Logger = Ref(Option<ILogger>.None);
    public static Ref<Option<AudioOutputIO>> AudioOutput = Ref(Option<AudioOutputIO>.None);


    static WaveeCore()
    {
        Runtime = WaveeRuntime.New(Logger, AudioOutput);

        Logger.OnChange().Subscribe(c =>
        {
            Runtime.Env.Logger = c.IfNone(NullLogger.Instance);
        });
        AudioOutput.OnChange().Subscribe(c =>
        {
            Runtime.Env.AudioOutputIo = c;
        });
    }

    public static WaveeRuntime Runtime { get; }
}