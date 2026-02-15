using Metaphrase.Primitives;

namespace Metaphrase;

/// <summary>
/// Options for configuring the translation service.
/// </summary>
public sealed record TranslateServiceOptions
{
    /// <summary>
    /// Gets the current language to use for translations.
    /// </summary>
    public string Current { get; init; } = string.Empty;

    /// <summary>
    /// Gets the fallback language to use for translations.
    /// </summary>
    public string Fallback { get; init; } = string.Empty;

    /// <summary>
    /// Gets an array of available language codes with translations.
    /// </summary>
    public string[] Available { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether to emit change notifications when translations are updated.
    /// </summary>
    public bool EmitChanges { get; init; } = true;
}
