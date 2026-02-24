using Metaphrase.Primitives.Internal;
using System.Runtime.CompilerServices;

namespace Metaphrase.Primitives;

/// <summary>
/// Manages a collection of language translations.
/// </summary>
/// <remarks>Language keys are compared using <see cref="StringComparer.OrdinalIgnoreCase"/>.</remarks>
public sealed class Languages
{
    private readonly ConcurrentLazyDictionary<string, Translations> store;

    /// <summary>
    /// Initializes a new instance of the <see cref="Languages"/> class.
    /// </summary>
    public Languages()
    {
        store = new(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Languages"/> class with the specified initial languages and their translations.
    /// </summary>
    /// <param name="languages">The initial languages and their translations.</param>
    public Languages(IDictionary<string, Translations> languages)
    {
        store = new(languages, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the collection contains the specified language key.
    /// </summary>
    /// <param name="language">The language key to locate in the collection.</param>
    /// <returns>true if the collection contains an element with the specified key; otherwise, false.</returns>
    public bool Contains(string language)
    {
        return store.ContainsKey(language);
    }

    /// <summary>
    /// Tries to get the translations for the specified language key.
    /// </summary>
    /// <param name="language">The language key.</param>
    /// <param name="result">When this method returns, contains the translations associated with the specified key, if the key is found; otherwise, null.</param>
    /// <returns>true if the language key is found; otherwise, false.</returns>
    public bool TryGet(string language, [NotNullWhen(true)] out Translations? result)
    {
        if (store.TryGetValue(language, out var lazy))
        {
            result = lazy.Value;
            return true;
        }
        Unsafe.SkipInit(out result);
        return false;
    }

    /// <summary>
    /// Gets the translations for the specified language key.
    /// </summary>
    /// <param name="language">The language key.</param>
    /// <returns>The translations for the specified language key.</returns>
    public Translations Get(string language)
    {
        return store.GetOrAdd(language, key => new());
    }

    /// <summary>
    /// Sets the translations for the specified language key.
    /// </summary>
    /// <param name="language">The language key.</param>
    /// <param name="value">The translations to set.</param>
    /// <param name="merge">true to merge the translations with existing translations; false to replace them entirely.</param>
    public void Set(string language, Translations value, bool merge = false)
    {
        store.AddOrUpdate(
            key: language,
            addFactory: key => value,
            updateFactory: (key, previous) => merge ? previous.Merge(value) : value
        );
    }

    /// <summary>
    /// Removes the translations for the specified language key.
    /// </summary>
    /// <param name="language">The language key.</param>
    public void Remove(string language)
    {
        store.TryRemove(language, out _);
    }
}
