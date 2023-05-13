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
namespace Wavee;

public static class WaveeCore
{
    //  internal static Atom<Seq<WeakReference<IAudioCore>>> Cores = Atom(LanguageExt.Seq<WeakReference<IAudioCore>>.Empty);
    internal static Ref<Option<ILogger>> Logger = Ref(Option<ILogger>.None);
    internal static Ref<Option<AudioOutputIO>> AudioOutput = Ref(Option<AudioOutputIO>.None);

    static WaveeCore()
    {
        Runtime = WaveeRuntime.New(Logger, AudioOutput);

        Logger.OnChange().Subscribe(c => { Runtime.Env.Logger = c.IfNone(NullLogger.Instance); });
        AudioOutput.OnChange().Subscribe(c => { Runtime.Env.AudioOutputIo = c; });
    }

    public static WaveeRuntime Runtime { get; }

    // public static void AddCoreClient(IAudioCore core)
    // {
    //     Cores.Swap(c => c.Add(new WeakReference<IAudioCore>(core)));
    // }
}