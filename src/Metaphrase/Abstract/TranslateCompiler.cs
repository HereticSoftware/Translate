using Metaphrase.Primitives;

namespace Metaphrase.Abstract;

/// <summary>
/// Class for compiling translations.
/// </summary>
public abstract class TranslateCompiler
{
    /// <summary>
    /// Compiles the translation for the specified language.
    /// </summary>
    /// <param name="language">The language for which to compile the translations.</param>
    /// <param name="translation">The translation to compile.</param>
    /// <returns>The compiled translation string.</returns>
    public abstract Observable<string> Compile(string language, string translation);

    /// <summary>
    /// Compiles the provided translations for the specified language.
    /// </summary>
    /// <param name="language">The language for which to compile the translations.</param>
    /// <param name="translations">The translations to compile.</param>
    /// <returns>The compiled <see cref="Translations"/>.</returns>
    public abstract Observable<Translations> Compile(string language, Translations translations);
}
