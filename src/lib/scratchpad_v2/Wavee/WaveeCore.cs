using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Wavee.Infrastructure.Live;

[assembly: InternalsVisibleTo("Wavee.Spotify")]

namespace Wavee;

internal static class WaveeCore
{
    public static Ref<Option<ILogger>> Logger = Ref(Option<ILogger>.None);

    
    static WaveeCore()
    {
        Runtime = WaveeRuntime.New(Logger);

        Logger.OnChange().Subscribe(c =>
        {
            Runtime.Env.Logger = c.IfNone(NullLogger.Instance);
        });
    }

    public static WaveeRuntime Runtime { get; }
}