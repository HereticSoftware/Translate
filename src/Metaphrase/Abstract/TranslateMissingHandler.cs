namespace Metaphrase.Abstract;

/// <summary>
/// Class for handling missing translations.
/// </summary>
public abstract class TranslateMissingHandler
{
    /// <summary>
    /// Handles the case when a translation is missing for the specified language and key.
    /// </summary>
    /// <param name="language">The language code for which the translation is missing.</param>
    /// <param name="key">The translation key that was not found.</param>
    /// <returns>An observable containing the string for the missing translation.</returns> 
    public abstract Observable<string> Handle(string language, string key);
}
