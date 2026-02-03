using Metaphrase.Primitives.Events;
using Metaphrase.Primitives.Internal;

namespace Metaphrase.Primitives;

/// <summary>
/// Manages a collection of language translations.
/// </summary>
/// <remarks>Language keys are compared using <see cref="StringComparer.OrdinalIgnoreCase"/></remarks>
public sealed class Languages : IDisposable
{
    private readonly SubjectWrapper<LanguageTranslationChangeEvent> onTranslationChange = new();
    private readonly Dictionary<string, Translations> store;

    /// <summary>
    /// Gets an observable sequence that notifies when a language translation changes.
    /// </summary>
    public Observable<LanguageTranslationChangeEvent> OnTranslationChange => onTranslationChange.GetObservable();

    /// <summary>
    /// Initializes a new instance of the <see cref="Languages"/> class.
    /// </summary>
    public Languages()
    {
        store = new(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Languages"/> class with the specified initial languages.
    /// </summary>
    /// <param name="languages">The initial languages and their translations.</param>
    public Languages(IDictionary<string, Translations> languages)
    {
        store = new(languages, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the collection contains the specified language key.
    /// </summary>
    /// <param name="key">The language key to locate in the collection.</param>
    /// <returns>true if the collection contains an element with the specified key; otherwise, false.</returns>
    public bool Contains(string key)
    {
        return store.ContainsKey(key);
    }

    /// <summary>
    /// Tries to get the translations for the specified language key.
    /// </summary>
    /// <param name="key">The language key.</param>
    /// <param name="result">When this method returns, contains the translations associated with the specified key, if the key is found; otherwise, null.</param>
    /// <returns>true if the language key is found; otherwise, false.</returns>
    public bool TryGet(string key, [NotNullWhen(true)] out Translations? result)
    {
        return store.TryGetValue(key, out result);
    }

    /// <summary>
    /// Gets the translations for the specified language key.
    /// </summary>
    /// <param name="key">The language key.</param>
    /// <returns>The translations for the specified language key.</returns>
    public Translations Get(string key)
    {
        if (store.TryGetValue(key, out var translations))
        {
            return translations;
        }
        translations = new();
        Set(key, translations);
        return translations;
    }

    /// <summary>
    /// Sets the translations for the specified language key.
    /// </summary>
    /// <param name="key">The language key.</param>
    /// <param name="value">The translations to set.</param>
    public void Set(string key, Translations value)
    {
        Remove(key);
        store.Add(key, value);
        onTranslationChange.SetSub(key, value.OnTranslationChange.Select(on => new LanguageTranslationChangeEvent(key, on.Key, on.Translation)).Subscribe(onTranslationChange.GetObserver()));
    }

    /// <summary>
    /// Removes the translations for the specified language key.
    /// </summary>
    /// <param name="key">The language key.</param>
    public void Remove(string key)
    {
        store.Remove(key);
        onTranslationChange.RemoveSub(key);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        onTranslationChange.Dispose();
    }
}
