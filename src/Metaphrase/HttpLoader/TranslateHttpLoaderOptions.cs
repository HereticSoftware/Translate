namespace Metaphrase.HttpLoader;

/// <summary>
/// Options for configuring the translation HTTP loader.
/// </summary>
public sealed record TranslateHttpLoaderOptions
{
    /// <summary>
    /// Gets or sets the prefix for the translation files' path.
    /// Default value is "./i18n/".
    /// </summary>
    public string Prefix { get; init; } = "./i18n/";

    /// <summary>
    /// Gets or sets the suffix for the translation files' path.
    /// Default value is ".json".
    /// </summary>
    public string Suffix { get; init; } = ".json";
}
