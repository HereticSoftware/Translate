namespace Metaphrase;

/// <summary>
/// Options for configuring the translation service.
/// </summary>
public sealed record TranslateServiceOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to use the default language translation
    /// when the current language translation is missing.
    /// </summary>
    /// <value>
    /// <c>true</c> if the default language translation should be used; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>Default is <c>false</c>.</remarks>
    public bool UseDefaultLang { get; init; }

    /// <summary>
    /// Gets or sets the default language to use for translations.
    /// </summary>
    /// <value>
    /// The default language code as a <see cref="string"/>.
    /// </value>
    /// <remarks>
    /// The default value is an empty string.
    /// </remarks>
    public string DefaultLanguage { get; init; } = string.Empty;
}
