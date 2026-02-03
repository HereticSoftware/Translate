namespace Metaphrase.Primitives.Events;

/// <summary>
/// Represents an event that occurs when the language changes.
/// </summary>
/// <param name="Lang">The new language code.</param>
/// <param name="Translations">The translations associated with the new language.</param>
public record LanguageChangeEvent(string Lang, Translations Translations);
