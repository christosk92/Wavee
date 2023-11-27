using LanguageExt;

namespace Wavee.Domain.Playback.Player;

public sealed class WaveePlaybackList
{
    private readonly LinkedList<Func<ValueTask<IWaveeMediaSource>>> _factory;
    private readonly Either<InfiniteContext, int> _count;

    private WaveePlaybackList(Func<int, ValueTask<IWaveeMediaSource>> factory,
        Either<InfiniteContext, int> count)
    {
        if (count.IsLeft)
        {
            //TODO:
        }
        else
        {
            var countVal = count.Match(
                Left: _ => throw new InvalidOperationException(),
                Right: x => x
            );

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
            count: sources.Length);
    }

    private readonly record struct InfiniteContext;
}