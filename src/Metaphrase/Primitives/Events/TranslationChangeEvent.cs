namespace Metaphrase.Primitives.Events;

/// <summary>
/// Represents an event that indicates a change in translation.
/// </summary>
/// <param name="Key">The key of the translation that has changed.</param>
/// <param name="Translation">The new translation value.</param>
public record TranslationChangeEvent(string Key, string Translation)
{
    /// <summary>
    /// Gets a static instance of <see cref="TranslationChangeEvent"/> representing multiple changes.
    /// </summary>
    public static TranslationChangeEvent Multiple { get; } = new(string.Empty, string.Empty);

    /// <summary>
    /// Gets a value indicating whether this instance represents multiple changes.
    /// </summary>
    public bool IsMultiple => this == Multiple;
}
