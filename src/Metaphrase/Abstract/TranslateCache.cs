using Metaphrase.Primitives;

namespace Metaphrase.Abstract;

/// <summary>
/// Class for translation caching. Provides synchronous and asynchronous methods for storing and retrieving translations.
/// </summary>
public abstract class TranslateCache
{
    /// <summary>
    /// Get immediately all translations for the specified language.
    /// </summary>
    /// <param name="language">The language key.</param>
    /// <returns>The translations for the specified language.</returns>
    public abstract Translations Instant(string language);

    /// <summary>
    /// Get immediately a translation value for the specified language and key.
    /// </summary>
    /// <param name="language">The language key.</param>
    /// <param name="key">The translation key.</param>
    /// <returns>The translation value for the specified language and key.</returns>
    public abstract string Instant(string language, string key);

    /// <summary>
    /// Try to immediately get a translation value for the specified language and key.
    /// </summary>
    /// <param name="language">The language key.</param>
    /// <param name="key">The translation key.</param>
    /// <param name="value">When this method returns, contains the translation value if found; otherwise, null.</param>
    /// <returns><see langword="true"/> if the translation was found; otherwise, <see langword="false"/>.</returns>
    public abstract bool TryInstant(string language, string key, [NotNullWhen(true)] out string? value);

    /// <summary>
    /// Get the translations for the specified language.
    /// </summary>
    /// <param name="language">The language key.</param>
    /// <returns>The translations for the specified language key.</returns>
    public abstract Observable<Translations> Get(string language);

    /// <summary>
    /// Get a translation value for the specified language and key.
    /// </summary>
    /// <param name="language">The language key.</param>
    /// <param name="key">The translation key.</param>
    /// <returns>An observable that emits the translation value for the specified language and key.</returns>
    public abstract Observable<string> Get(string language, string key);

    /// <summary>
    /// Set the translations for the specified language.
    /// </summary>
    /// <param name="language">The language key.</param>
    /// <param name="translations">The translations to set.</param>
    /// <param name="merge">If <see langword="true"/>, merges the translations with existing ones; otherwise, replaces them.</param>
    /// <returns>An observable that completes when the operation is finished.</returns>
    public abstract Observable<Unit> Set(string language, Translations translations, bool merge = false);

    /// <summary>
    /// Set a translation value for the specified language and key.
    /// </summary>
    /// <param name="language">The language key.</param>
    /// <param name="key">The translation key.</param>
    /// <param name="value">The translation value to set.</param>
    /// <returns>An observable that completes when the operation is finished.</returns>
    public abstract Observable<Unit> Set(string language, string key, string value);

    /// <summary>
    /// Remove the translations for the specified language.
    /// </summary>
    /// <param name="language">The language key.</param>
    /// <returns>An observable that completes when the operation is finished.</returns>
    public abstract Observable<Unit> Remove(string language);

    /// <summary>
    /// Remove a translation value for the specified language and key.
    /// </summary>
    /// <param name="language">The language key.</param>
    /// <param name="key">The translation key.</param>
    /// <returns>An observable that completes when the operation is finished.</returns>
    public abstract Observable<Unit> Remove(string language, string key);
}
