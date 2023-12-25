using Wavee.Interfaces;

namespace Wavee.Core.Playback;

public sealed class WaveePlaybackItem
{
    public required Func<ValueTask<IWaveeMediaSource>> Factory { get; init; }
    public required string? Id { get; init; }
}

public sealed class WaveePlaybackList
{
    private readonly Func<ValueTask<LinkedList<WaveePlaybackItem>>> _factory;

    private WaveePlaybackList(IWaveePlaybackContextCount count, params WaveePlaybackItem[] initialItems)
    {
        _factory = () => new ValueTask<LinkedList<WaveePlaybackItem>>(new LinkedList<WaveePlaybackItem>(initialItems));
        Count = count;
    }

    private WaveePlaybackList(WaveePlaybackAdaptiveCount count, Func<Task<WaveePlaybackItem[]>>? initialItems)
    {
        _factory = () => CreateLinkedList(initialItems);
        Count = count;
    }

    private async ValueTask<LinkedList<WaveePlaybackItem>> CreateLinkedList(Func<Task<WaveePlaybackItem[]>>? initialItems)
    {
        var items = await initialItems!();
        return new LinkedList<WaveePlaybackItem>(items);
    }

    public IWaveePlaybackContextCount Count { get; }

    public string Id { get; }
    public string? Name { get; }
    public string? Description { get; }

    public ValueTask<LinkedListNode<WaveePlaybackItem>?> Get(int index)
    {
        var factory = _factory();
        if (factory.IsCompletedSuccessfully)
        {
            return new ValueTask<LinkedListNode<WaveePlaybackItem>?>(GetSync(factory.Result, index));
        }

        return new ValueTask<LinkedListNode<WaveePlaybackItem>?>(GetAsync(factory, index));
    }

    private async Task<LinkedListNode<WaveePlaybackItem>?> GetAsync(ValueTask<LinkedList<WaveePlaybackItem>> valueTask,
        int index)
    {
        var factory = await valueTask;
        return GetSync(factory, index);
    }

    private LinkedListNode<WaveePlaybackItem>? GetSync(LinkedList<WaveePlaybackItem> factoryResult, int index)
    {
        int i = 0;
        var node = factoryResult.First;
        while (i < index)
        {
            node = node.Next;
            i++;
        }

        return node;
    }
    
    public static WaveePlaybackList Create(params IWaveeMediaSource[] sources)
    {
        return new WaveePlaybackList(
            count: WaveePlaybackContextCount.Create(sources.Length),
            sources.Select(source => new WaveePlaybackItem
            {
                Factory = () => new ValueTask<IWaveeMediaSource>(source),
                Id = source.Item.Id
            }).ToArray());
    }

    public static WaveePlaybackList Create(int count, params WaveePlaybackItem[] items)
    {
        return new WaveePlaybackList(
            count: WaveePlaybackContextCount.Create(count),
            items
            );
    }
    public static WaveePlaybackList Create(params WaveePlaybackItem[] items)
    {
        return new WaveePlaybackList(
            count: WaveePlaybackContextCount.Adaptive,
            items
        );
    }
    public static WaveePlaybackList Create(Func<Task<WaveePlaybackItem[]>>? sources)
    {
        return new WaveePlaybackList(
            count: WaveePlaybackContextCount.Adaptive,
            sources
        );
    }


    private readonly record struct InfiniteContext;
}

internal sealed class WaveePlaybackContextCount : IWaveePlaybackContextCount
{
    private readonly int _count;
    private WaveePlaybackContextCount(int count)
    {
        _count = count;
    }
    

    public static WaveePlaybackInfiniteContextCount Infinite { get; } = new WaveePlaybackInfiniteContextCount();
    public static WaveePlaybackContextCount Create(int count) => new(count);
    public static WaveePlaybackAdaptiveCount Adaptive { get; set; }
    public ValueTask<int> GetCount()
    {
        return new ValueTask<int>(_count);
    }
}

internal sealed class WaveePlaybackInfiniteContextCount : IWaveePlaybackContextCount
{
    public ValueTask<int> GetCount()
    {
        throw new NotSupportedException("Infinite context does not have a count");
    }
}
internal sealed class WaveePlaybackAdaptiveCount : IWaveePlaybackContextCount
{
    public ValueTask<int> GetCount()
    {
        throw new NotImplementedException();
    }
}

public interface IWaveePlaybackContextCount
{
    ValueTask<int> GetCount();
}