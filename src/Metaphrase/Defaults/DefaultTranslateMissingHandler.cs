using Metaphrase.Abstract;

namespace Metaphrase.Defaults;

/// <summary>
/// A default implementation for fallback that awlays returns key when a translation is missing.
/// </summary>
public sealed class DefaultTranslateMissingHandler : TranslateMissingHandler
{
    /// <summary>
    /// Gets the singleton instance of the <see cref="DefaultTranslateMissingHandler"/>.
    /// </summary>
    public static DefaultTranslateMissingHandler Instance { get; } = new();

    /// <inheritdoc/>
    public override Observable<string> Handle(string language, string key)
    {
        return Observable.Return(key);
    }
}
