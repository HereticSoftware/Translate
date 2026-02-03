using Metaphrase.Primitives;

namespace Metaphrase.Abstract;

/// <summary>
/// Class for compiling translations.
/// </summary>
public abstract class TranslateCompiler
{
    /// <summary>
    /// Compiles the provided translations for the specified language.
    /// </summary>
    /// <param name="translations">The translations to compile.</param>
    /// <param name="lang">The language for which to compile the translations.</param>
    /// <returns>A <see cref="Translations"/> object containing the compiled translations.</returns>
    public abstract Translations CompileTranslations(Translations translations, string lang);
}
