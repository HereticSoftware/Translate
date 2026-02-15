namespace Metaphrase.Primitives.Events;

/// <summary>
/// Represents an event that occurs when the language changes.
/// </summary>
/// <param name="Language">The new language code.</param>
public record LanguageChangeEvent(string Language);
