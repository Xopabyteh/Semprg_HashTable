using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

BenchmarkRunner.Run<Benchmarks>();

class MyHashTable<TKey, TValue>
    where TKey : IEquatable<TKey>
{
    private int _capacity;
    private ChainingSlot[] _slots;
    
    public MyHashTable(int capacity)
    {
        _capacity = capacity;
        _slots = new ChainingSlot[capacity];

        for (int i = 0; i < _capacity; i++)
        {
            _slots[i] = new ChainingSlot();
        }
    }
    
    public void Add(TKey key, TValue value)
    {
        var slot = GetSlot(key);
        slot.Add(key, value);
    }

    public TValue? Get(TKey key)
    {
        var slot = GetSlot(key);
        var value = slot.Get(key);

        return value;
    }

    public void TryRemove(TKey key)
    {
        var slot = GetSlot(key);
        slot.Remove(key);
    }

    private int GetSlotIndex(TKey key) 
    {
        var hashCode = Math.Abs(key.GetHashCode());
        var index = hashCode % _slots.Length;
        return index;
    }

    private ChainingSlot GetSlot(TKey key)
        => _slots[GetSlotIndex(key)];

    /// <summary>
    /// The hashtable get's a slot at the hascode of the key stores/retrives
    /// data from the slot.
    /// </summary>
    class ChainingSlot
    {
        private List<KeyValuePair<TKey, TValue>> _items = new(13);
    
        public void Add(TKey key, TValue value)
        {
            _items.Add(new KeyValuePair<TKey, TValue>(key, value));
        }

        public TValue? Get(TKey key)
        {
            //return _items.FirstOrDefault(kvp => kvp.Key!.Equals(key)).Value;
            //foreach (var item in _items)
            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (item.Key!.Equals(key))
                {
                    return item.Value;
                }
            }

            return default;
        }

        public void Remove(TKey key)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (item.Key!.Equals(key))
                {
                    _items.RemoveAt(i);
                    return;
                }
            }
        }
    }
}

[MemoryDiagnoser]
[Orderer]
public class Benchmarks 
{
    private const int Capacity = 1_000;

    private Dictionary<Guid, int> _netDict = new(Capacity);
    private MyHashTable<Guid, int> _myHashTable = new(Capacity);
    private Guid[] _searchData;
    private KeyValuePair<Guid, int>[] _dataToAdd;

    [IterationSetup]
    public void Setup()
    {
        var data = Enumerable.Range(0, Capacity)
            .Select(i => Guid.NewGuid())
            .ToArray();

        // Other half of the data is used for searching
        _searchData = data.AsSpan(Capacity / 2).ToArray();

        for (int i = 0; i < data.Length; i++)
        {
            var key = data[i];
            var value = i;

            _netDict.Add(key, value);
            _myHashTable.Add(key, value);
        }

        _dataToAdd = Enumerable.Range(0, Capacity)
            .Select(i => new KeyValuePair<Guid, int>(Guid.NewGuid(), i + Capacity))
            .ToArray();
    }

    [Benchmark]
    public void NetGet()
    {
        for (int i = 0; i < _searchData.Length; i++)
        {
            var key = _searchData[i];
            var value = _netDict[key];
        }
    }
    [Benchmark]
    public void MyGet()
    {
        for (int i = 0; i < _searchData.Length; i++)
        {
            var key = _searchData[i];
            var value = _myHashTable.Get(key);
        }
    }


    [Benchmark]
    public void MyRemove()
    {
        for (int i = 0; i < _searchData.Length; i++)
        {
            var key = _searchData[i];
            _myHashTable.TryRemove(key);
        }
    }

    [Benchmark]
    public void NetRemove()
    {
        for (int i = 0; i < _searchData.Length; i++)
        {
            var key = _searchData[i];
            _netDict.Remove(key);
        }
    }

    [Benchmark]
    public void MyAdd()
    {
        for (int i = 0; i < _searchData.Length; i++)
        {
            var kvp = _dataToAdd[i];
            _myHashTable.Add(kvp.Key, kvp.Value);
        }
    }

    [Benchmark]
    public void NetAdd()
    {
        for (int i = 0; i < _searchData.Length; i++)
        {
            var kvp = _dataToAdd[i];
            _netDict.Add(kvp.Key, kvp.Value);
        }
    }
}