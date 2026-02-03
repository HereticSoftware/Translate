using Metaphrase.Primitives;

namespace Metaphrase.Abstract;

/// <summary>
/// Class for loading translations.
/// </summary>
public abstract class TranslateLoader
{
    /// <summary>
    /// Gets the translations for the specified language.
    /// </summary>
    /// <param name="lang">The language code for which to get translations.</param>
    /// <returns>An observable sequence of translations.</returns>
    public abstract Observable<Translations> GetTranslation(string lang);
}
