using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Wavee.Core.Contracts;
using Wavee.Core.Infrastructure.Traits;

namespace Wavee.Core.Infrastructure.Sys.IO;

public static class AudioOutput<RT>
    where RT : struct, HasCancel<RT>, HasAudioOutput<RT>
{
    /// <summary>
    /// Resume audio playback. Aka resuming.
    /// </summary>
    /// <returns></returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Eff<RT, TimeSpan> Start() =>
        from _ in default(RT).AudioOutputEff.Map(e => e.IfSome(x => x.Start()))
        from pos in default(RT).AudioOutputEff.Map(e =>
            e.Match(
                Some: x => x.Position(),
                None: () => TimeSpan.Zero))
        select pos;
    /// <summary>
    /// Halt current audio playback. Aka pausing.
    /// </summary>
    /// <returns></returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Eff<RT, TimeSpan> Pause() =>
        from _ in default(RT).AudioOutputEff.Map(e =>
            e.IfSome(x => x.Pause()))
        from pos in default(RT).AudioOutputEff.Map(e =>
            e.Match(
                Some: x => x.Position(),
                None: () => TimeSpan.Zero))
        select pos;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Eff<RT, Task> PlayStream(Stream stream,
        Action<TimeSpan> onPositionChanged,
        bool closeOtherStreams) =>
        from handle in default(RT).AudioOutputEff.Map(e => e.Match(
            Some: x => x.PlayStream(stream, onPositionChanged, closeOtherStreams),
            None: () => Task.CompletedTask))
        select handle;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Eff<RT, Unit> Seek(TimeSpan seekPosition) =>
        from _ in default(RT).AudioOutputEff.Map(e => e.IfSome(x => x.Seek(seekPosition)))
        select unit;
    
    public static Eff<RT, TimeSpan> Position =>
        from pos in default(RT).AudioOutputEff.Map(e =>
            e.Match(
                Some: x => x.Position(),
                None: () => TimeSpan.Zero))
        select pos;
}