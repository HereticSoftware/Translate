using System.Runtime.CompilerServices;

namespace Metaphrase.Primitives.Internal;

internal sealed class ConcurrentLazyDictionary<TKey, TValue> : ConcurrentDictionary<TKey, Lazy<TValue>> where TKey : notnull
{
    public ConcurrentLazyDictionary()
    {
    }

    public ConcurrentLazyDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection) : base(ToLazy(collection))
    {
    }

    public ConcurrentLazyDictionary(IEqualityComparer<TKey>? comparer) : base(comparer)
    {
    }

    public ConcurrentLazyDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey>? comparer) : base(ToLazy(collection), comparer)
    {
    }

    public ConcurrentLazyDictionary(int concurrencyLevel, int capacity) : base(concurrencyLevel, capacity)
    {
    }

    public ConcurrentLazyDictionary(int concurrencyLevel, IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey>? comparer) : base(concurrencyLevel, ToLazy(collection), comparer)
    {
    }

    public ConcurrentLazyDictionary(int concurrencyLevel, int capacity, IEqualityComparer<TKey>? comparer) : base(concurrencyLevel, capacity, comparer)
    {
    }

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
    {
        var lazy = GetOrAdd(
            key: key,
            key => new Lazy<TValue>(() => factory(key))
        );
        return lazy.Value;
    }

    public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addFactory, Func<TKey, TValue, TValue> updateFactory)
    {
        var lazy = AddOrUpdate(
            key: key,
            addValueFactory: key => new Lazy<TValue>(() => addFactory(key)),
            updateValueFactory: (key, old) => new Lazy<TValue>(() => updateFactory(key, old.Value))
        );
        return lazy.Value;
    }

    public bool TryGet(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        if (TryGetValue(key, out var lazy))
        {
            value = lazy.Value;
            return true;
        }
        Unsafe.SkipInit(out value);
        return false;
    }

    private static IEnumerable<KeyValuePair<TKey, Lazy<TValue>>> ToLazy(IEnumerable<KeyValuePair<TKey, TValue>> collection)
    {
        return collection.Select(kv => KeyValuePair.Create(kv.Key, new Lazy<TValue>(kv.Value)));
    }
}
