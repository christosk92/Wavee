using System.Collections;

namespace Wavee.Playback.Streaming;

internal class LruCache<TKey, TValue>
{
    private readonly int _capacity;
    private readonly Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>> _cacheMap;
    private readonly LinkedList<KeyValuePair<TKey, TValue>> _lruList;

    public LruCache(int capacity)
    {
        _capacity = capacity;
        _cacheMap = new Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>>();
        _lruList = new LinkedList<KeyValuePair<TKey, TValue>>();
    }

    public IEnumerable<TValue> Values
    {
        get
        {
            lock (this)
            {
                foreach (var node in _lruList)
                {
                    yield return node.Value;
                }
            }
        }
    }

    public TValue Get(TKey key)
    {
        lock (this)
        {
            if (_cacheMap.TryGetValue(key, out var node))
            {
                _lruList.Remove(node);
                _lruList.AddFirst(node);
                return node.Value.Value;
            }
            return default;
        }
    }
    /// <summary>
    /// Checks if the cache contains the specified key.
    /// </summary>
    /// <param name="key">The key to locate in the cache.</param>
    /// <returns>true if the cache contains an element with the specified key; otherwise, false.</returns>
    public bool ContainsKey(TKey key)
    {
        lock (this)
        {
            return _cacheMap.ContainsKey(key);
        }
    }
    public void Add(TKey key, TValue value)
    {
        lock (this)
        {
            if (_cacheMap.TryGetValue(key, out var node))
            {
                _lruList.Remove(node);
                _cacheMap.Remove(key);
            }
            else if (_cacheMap.Count >= _capacity)
            {
                // Remove the least recently used item
                var lru = _lruList.Last;
                if (lru != null)
                {
                    _cacheMap.Remove(lru.Value.Key);
                    _lruList.RemoveLast();
                }
            }
            var newNode = new LinkedListNode<KeyValuePair<TKey, TValue>>(new KeyValuePair<TKey, TValue>(key, value));
            _lruList.AddFirst(newNode);
            _cacheMap[key] = newNode;
        }
    }
}