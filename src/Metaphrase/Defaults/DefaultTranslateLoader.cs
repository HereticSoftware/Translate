using Metaphrase.Abstract;
using Metaphrase.Primitives;

namespace Metaphrase.Defaults;

/// <summary>
/// Provides a default implementation for loading translations that always returns an empty translations object.
/// </summary>
public sealed class DefaultTranslateLoader : TranslateLoader
{
    /// <summary>
    /// Gets the singleton instance of the <see cref="DefaultTranslateLoader"/> class.
    /// </summary>
    public static DefaultTranslateLoader Instance { get; } = new();

    /// <inheritdoc/>
    public override Observable<Translations> GetTranslation(string lang)
    {
        return Observable.Return(new Translations());
    }
}
