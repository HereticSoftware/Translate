using Metaphrase.Primitives;
using System.Runtime.CompilerServices;

namespace Metaphrase.Abstract;

/// <summary>
/// A default implementation of the <see cref="TranslateCache"/> class that uses an in-memorry store.
/// </summary>
public sealed class DefaultTranslateCache : TranslateCache
{
    private readonly Languages languages = new();

    /// <inheritdoc/>
    public override Translations Instant(string language)
    {
        return languages.Get(language);
    }

    /// <inheritdoc/>
    public override string Instant(string language, string key)
    {
        return languages.Get(language).Get(key);
    }

    /// <inheritdoc/>
    public override bool TryInstant(string language, string key, [NotNullWhen(true)] out string? value)
    {
        Unsafe.SkipInit(out value);
        return languages.TryGet(language, out var translations) && translations.TryGet(key, out value);
    }

    /// <inheritdoc/>
    public override Observable<Translations> Get(string language)
    {
        return Observable.Return(Instant(language));
    }

    /// <inheritdoc/>
    public override Observable<string> Get(string language, string key)
    {
        return Observable.Return(Instant(language, key));
    }

    /// <inheritdoc/>
    public override Observable<Unit> Set(string language, Translations translations, bool merge = false)
    {
        languages.Set(language, translations, merge);
        return Observable.Return(Unit.Default);
    }

    /// <inheritdoc/>
    public override Observable<Unit> Set(string language, string key, string value)
    {
        languages.Get(language).Set(key, value);
        return Observable.Return(Unit.Default);
    }

    /// <inheritdoc/>
    public override Observable<Unit> Remove(string language)
    {
        languages.Remove(language);
        return Observable.Return(Unit.Default);
    }

    /// <inheritdoc/>
    public override Observable<Unit> Remove(string language, string key)
    {
        languages.Get(language).Remove(key);
        return Observable.Return(Unit.Default);
    }
}
