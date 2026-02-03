namespace Metaphrase.Primitives.Events;

/// <summary>
/// Represents an event that occurs when a language translation changes.
/// </summary>
/// <param name="Lang">The language code of the translation.</param>
/// <param name="Key">The key associated with the translation.</param>
/// <param name="Translation">The translated text.</param>
public record LanguageTranslationChangeEvent(string Lang, string Key, string Translation);
