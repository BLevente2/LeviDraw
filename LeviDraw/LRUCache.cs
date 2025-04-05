namespace LeviDraw;

internal class LruCache<K, V> where K : notnull
{
    private readonly int _capacity;
    private readonly Dictionary<K, LinkedListNode<CacheItem>> _dict;
    private readonly LinkedList<CacheItem> _list;
    private readonly object _lock = new object();

    internal class CacheItem
    {
        internal K Key { get; }
        internal V Value { get; set; }
        internal CacheItem(K key, V value)
        {
            Key = key;
            Value = value;
        }
    }

    internal LruCache(int capacity)
    {
        _capacity = capacity;
        _dict = new Dictionary<K, LinkedListNode<CacheItem>>();
        _list = new LinkedList<CacheItem>();
    }

    internal bool TryGetValue(K key, out V value)
    {
        lock (_lock)
        {
            if (_dict.TryGetValue(key, out var node))
            {
                _list.Remove(node);
                _list.AddLast(node);
                value = node.Value.Value;
                return true;
            }
            value = default!;
            return false;
        }
    }

    internal void Add(K key, V value)
    {
        lock (_lock)
        {
            if (_dict.ContainsKey(key))
            {
                var node = _dict[key];
                _list.Remove(node);
                _list.AddLast(node);
                node.Value.Value = value;
            }
            else
            {
                if (_dict.Count >= _capacity)
                {
                    var oldest = _list.First;
                    if (oldest != null)
                    {
                        _dict.Remove(oldest.Value.Key);
                        _list.RemoveFirst();
                    }
                }
                var newNode = new LinkedListNode<CacheItem>(new CacheItem(key, value));
                _list.AddLast(newNode);
                _dict[key] = newNode;
            }
        }
    }

    internal void Clear()
    {
        lock (_lock)
        {
            _dict.Clear();
            _list.Clear();
        }
    }

}