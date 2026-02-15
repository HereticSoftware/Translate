namespace Metaphrase.Primitives.Events;

/// <summary>
/// Represents an event that occurs when a language translation changes.
/// </summary>
/// <param name="Language">The language code of the translation.</param>
/// <param name="Key">The key associated with the translation.</param>
/// <param name="Value">The translated text.</param>
public record LanguageTranslationChangeEvent(string Language, string Key, string Value);
