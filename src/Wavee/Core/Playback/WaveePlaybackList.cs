using System.Collections;
using Wavee.Interfaces;

namespace Wavee.Core.Playback;

public sealed class WaveePlaybackItem
{
    public required Func<ValueTask<IWaveeMediaSource>> Factory { get; init; }
    public required string? Id { get; init; }
    public required IReadOnlyDictionary<string, string> Metadata { get; init; }
}

public sealed class WaveePlaybackList : IEnumerable<WaveePlaybackItem>
{
    private readonly Func<ValueTask<LinkedList<WaveePlaybackItem>>> _factory;
    private int _startFrom;

    private WaveePlaybackList(IWaveePlaybackContextCount count, params WaveePlaybackItem[] initialItems)
    {
        _factory = () => new ValueTask<LinkedList<WaveePlaybackItem>>(new LinkedList<WaveePlaybackItem>(initialItems));
        Count = count;
    }

    private WaveePlaybackList(WaveePlaybackAdaptiveCount count, Func<Task<(WaveePlaybackItem[] Items, int StartFrom)>>? initialItems)
    {
        _factory = () => CreateLinkedList(initialItems);
        Count = count;
    }

    private async ValueTask<LinkedList<WaveePlaybackItem>> CreateLinkedList(
        Func<Task<(WaveePlaybackItem[] Items, int StartFrom)>>? initialItems)
    {
        var items = await initialItems!();
        _startFrom = items.StartFrom;
        return new LinkedList<WaveePlaybackItem>(items.Items);
    }

    public IWaveePlaybackContextCount Count { get; }

    public string Id { get; }
    public string? Name { get; }
    public string? Description { get; }

    public ValueTask<LinkedListNode<WaveePlaybackItem>?> Get(int index, bool firstcall)
    {
        var factory = _factory();
        if (factory.IsCompletedSuccessfully)
        {
            if (firstcall)
            {
                index += _startFrom;
            }
            return new ValueTask<LinkedListNode<WaveePlaybackItem>?>(GetSync(factory.Result, index));
        }

        return new ValueTask<LinkedListNode<WaveePlaybackItem>?>(GetAsync(factory, index, firstcall));
    }

    private async Task<LinkedListNode<WaveePlaybackItem>?> GetAsync(ValueTask<LinkedList<WaveePlaybackItem>> valueTask,
        int index, bool firstcall)
    {
        var factory = await valueTask;
        if (firstcall)
        {
            index += _startFrom;
        }
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
                Id = source.Item.Id,
                Metadata = new Dictionary<string, string>()
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
    public static WaveePlaybackList Create(Func<Task<(WaveePlaybackItem[] Items, int StartFrom)>>? sources)
    {
        return new WaveePlaybackList(
            count: WaveePlaybackContextCount.Adaptive,
            sources
        );
    }


    private readonly record struct InfiniteContext;

    public IEnumerator<WaveePlaybackItem> GetEnumerator()
    {
        return new WaveePlaybackListEnumerator(_factory, Count);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    
    private sealed class WaveePlaybackListEnumerator : IEnumerator<WaveePlaybackItem>
    {
        private readonly Func<ValueTask<LinkedList<WaveePlaybackItem>>> _factory;
        private readonly IWaveePlaybackContextCount _count;
        
        private LinkedListNode<WaveePlaybackItem>? _current;
        public WaveePlaybackListEnumerator(Func<ValueTask<LinkedList<WaveePlaybackItem>>> factory, IWaveePlaybackContextCount count)
        {
            _factory = factory;
            _count = count;
        }

        public void Dispose()
        {
            
        }

        public bool MoveNext()
        {
            if (_current is null)
            {
                var factory = _factory();
                if (factory.IsCompletedSuccessfully)
                {
                    _current = factory.Result.First;
                }
                else
                {
                    _current = factory.AsTask().Result.First;
                }
                return true;
            }

            if (_current.Next is null)
            {
                return false;
            }

            _current = _current.Next;
            return true;
        }

        public void Reset()
        {
            _current = null;
        }

        public WaveePlaybackItem Current => _current!.Value;

        object IEnumerator.Current => Current;
    }
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