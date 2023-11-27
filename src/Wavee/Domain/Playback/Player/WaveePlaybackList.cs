using LanguageExt;

namespace Wavee.Domain.Playback.Player;

public sealed class WaveePlaybackList
{
    private readonly Func<int, IWaveeMediaSource> _factory;
    private readonly Either<InfiniteContext, ValueTask<int>> _count;

    private WaveePlaybackList(Func<int, IWaveeMediaSource> factory,
        Either<InfiniteContext, ValueTask<int>> count)
    {
        _factory = factory;
        _count = count;
    }
    public IWaveeMediaSource Get(int index)
    {
        return _factory(index);
    }

    public bool TryGet(int index, out IWaveeMediaSource source)
    {
        try
        {
            source = _factory(index);
            return true;
        }
        catch (IndexOutOfRangeException)
        {
            source = default!;
            return false;
        }
    }

    public static WaveePlaybackList Create(params IWaveeMediaSource[] sources)
    {
        return new WaveePlaybackList(
            factory: i => sources[i],
            count: new ValueTask<int>(sources.Length));
    }

    private readonly record struct InfiniteContext;
}