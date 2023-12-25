using Wavee.Interfaces;

namespace Wavee.Core.Playback;

public sealed class WaveePlaybackList
{
    private readonly LinkedList<Func<ValueTask<IWaveeMediaSource>>> _factory;
    private readonly WaveePlaybackContextCount _count;

    private WaveePlaybackList(Func<int, ValueTask<IWaveeMediaSource>> factory,
        WaveePlaybackContextCount count)
    {
        if (count.IsInfinite)
        {
            //TODO:
        }
        else
        {
            var countVal = count.Count;

            var list = new LinkedList<Func<ValueTask<IWaveeMediaSource>>>();
            for (var i = 0; i < countVal; i++)
            {
                var num = i;
                var factoryFunc = () => factory(num);
                list.AddLast(factoryFunc);
            }
            
            _factory = list;
        }

        _count = count;
    }

    public LinkedListNode<Func<ValueTask<IWaveeMediaSource>>>? Get(int index)
    {
        int i = 0;
        var node = _factory.First;
        while (i < index)
        {
            node = node.Next;
            i++;
        }

        return node;
    }

    public bool TryGet(int index, out LinkedListNode<Func<ValueTask<IWaveeMediaSource>>> source)
    {
        if (index < 0)
        {
            source = default!;
            return false;
        }

        var node = Get(index);
        if (node is null)
        {
            source = default!;
            return false;
        }

        source = node;
        return true;
    }

    public static WaveePlaybackList Create(params IWaveeMediaSource[] sources)
    {
        return new WaveePlaybackList(
            factory: i => new ValueTask<IWaveeMediaSource>(sources[i]),
            count: WaveePlaybackContextCount.Create(sources.Length));
    }

    public static WaveePlaybackList Create(Func<int, ValueTask<IWaveeMediaSource>> factory, int count)
    {
        return new WaveePlaybackList(
            factory: factory,
            count: WaveePlaybackContextCount.Create(count));
    }


    private readonly record struct InfiniteContext;
}

internal sealed class WaveePlaybackContextCount
{
    private WaveePlaybackContextCount(bool infinite, int count)
    {
        IsInfinite = infinite;
        Count = count;
    }

    public static WaveePlaybackContextCount Infinite { get; } = new(true, -1);
    public static WaveePlaybackContextCount Create(int count) => new(false, count);
    public bool IsInfinite { get; }
    public int Count { get; }
}