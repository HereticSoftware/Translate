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
    public override Observable<Translations> Set(string language, Translations translations, bool merge = false)
    {
        return Observable.Defer((languages, language, translations, merge), static s =>
        {
            s.languages.Set(s.language, s.translations, s.merge);
            return Observable.Return(s.translations);
        });
    }

    /// <inheritdoc/>
    public override Observable<string> Set(string language, string key, string value)
    {
        return Observable.Defer((languages, language, key, value), static s =>
        {
            s.languages.Get(s.language).Set(s.key, s.value);
            return Observable.Return(s.value);
        });
    }

    /// <inheritdoc/>
    public override Observable<Unit> Remove(string language)
    {
        return Observable.Defer((languages, language), static s =>
        {
            s.languages.Remove(s.language);
            return Observable.Return(Unit.Default);
        });
    }

    /// <inheritdoc/>
    public override Observable<Unit> Remove(string language, string key)
    {
        return Observable.Defer((languages, language, key), static s =>
        {
            s.languages.Get(s.language).Remove(s.key);
            return Observable.Return(Unit.Default);
        });
    }
}
