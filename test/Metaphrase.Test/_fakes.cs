namespace Metaphrase.Test;

/// <summary>
/// A fake implementation of <see cref="TranslateParser"/> used for testing.
/// </summary>
public sealed class FakeParser : TranslateParser
{
    private readonly Func<string, object?, string> interpolate;

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeParser"/> class.
    /// </summary>
    /// <param name="interpolate">
    /// The interpolation function that takes an expression string and optional parameters,
    /// and returns the interpolated result.
    /// </param>
    public FakeParser(Func<string, object?, string> interpolate)
    {
        this.interpolate = interpolate;
    }

    /// <inheritdoc/>
    public override string Interpolate(string expr, object? parameters)
    {
        return interpolate(expr, parameters);
    }
}

/// <summary>
/// A fake implementation of <see cref="TranslateLoader"/> used for testing translation loading behavior.
/// </summary>
public sealed class FakeLoader : TranslateLoader
{
    private int callCount;
    private int returnCount;
    private readonly Func<string, Observable<Translations>> factory;

    /// <summary>
    /// Gets the number of times <see cref="GetTranslation"/> has been called.
    /// </summary>
    public int CallCount => callCount;

    /// <summary>
    /// Gets the number of times a translation result has been returned to subscribers.
    /// </summary>
    public int ReturnCount => returnCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeLoader"/> class with empty translations.
    /// </summary>
    public FakeLoader() : this(new Translations())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeLoader"/> class with immediate translations.
    /// </summary>
    /// <param name="translations">The translations to return immediately.</param>
    public FakeLoader(Translations translations) : this(Observable.Defer(() => Observable.Return(translations)))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeLoader"/> class with a custom observable sequence.
    /// </summary>
    /// <param name="result">The observable sequence of translations to return.</param>
    public FakeLoader(Observable<Translations> result)
    {
        factory = (_) => result.Do(_ => Interlocked.Increment(ref returnCount));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeLoader"/> class with a custom factory function.
    /// </summary>
    /// <param name="factory">The factory function that takes a language code and returns an observable sequence of translations.</param>
    public FakeLoader(Func<string, Observable<Translations>> factory)
    {
        this.factory = factory;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeLoader"/> class with a specified delay and empty translations.
    /// </summary>
    /// <param name="delay">The time to delay before returning an empty <see cref="Translations"/> object.</param>
    public FakeLoader(TimeSpan delay) : this(delay, new Translations())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeLoader"/> class with a specified delay and translations.
    /// </summary>
    /// <param name="delay">The time to delay before returning the translations.</param>
    /// <param name="translations">The translations to return after the delay.</param>
    public FakeLoader(TimeSpan delay, Translations translations)
    {
        factory = (_) => Observable.Defer(() => Observable
            .Timer(delay)
            .Select(_ =>
            {
                Interlocked.Increment(ref returnCount);
                return translations;
            })
        );
    }

    /// <inheritdoc/>
    public override Observable<Translations> GetTranslation(string lang)
    {
        Interlocked.Increment(ref callCount);

        var observable = factory(lang);
        return observable;
    }

    /// <summary>
    /// Asserts that the call count and return count match the specified expected value.
    /// </summary>
    /// <param name="counts">The expected number of times <see cref="GetTranslation"/> should have been called and a translation result should have been returned.</param>
    public Task That(int counts)
    {
        return That(counts, counts);
    }

    /// <summary>
    /// Asserts that the call count and return count match the specified expected values.
    /// </summary>
    /// <param name="callCount">The expected number of times <see cref="GetTranslation"/> should have been called.</param>
    /// <param name="returnCount">The expected number of times a translation result should have been returned.</param>
    public async Task That(int callCount, int returnCount)
    {
        await Assert.That(CallCount).IsEqualTo(callCount);
        await Assert.That(ReturnCount).IsEqualTo(returnCount);
    }
}

/// <summary>
/// A fake implementation of <see cref="TranslateMissingHandler"/> used for testing missing translation handling behavior.
/// </summary>
public sealed class FakeMissingHandler : TranslateMissingHandler
{
    /// <summary>
    /// The handler function that processes missing translations.
    /// </summary>
    private readonly Func<string, string, Observable<string>> handle;

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeMissingHandler"/> class.
    /// </summary>
    /// <param name="handle">
    /// The handler function that takes a language code, translation key, and translate service,
    /// and returns a string to handle the missing translation.
    /// </param>
    public FakeMissingHandler(Func<string, string, string> handle)
    {
        this.handle = (language, key) => Observable.Return(handle(language, key));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeMissingHandler"/> class.
    /// </summary>
    /// <param name="handle">
    /// The handler function that takes a language code, translation key, and translate service,
    /// and returns an Observable of string to handle the missing translation.
    /// </param>
    public FakeMissingHandler(Func<string, string, Observable<string>> handle)
    {
        this.handle = handle;
    }

    /// <inheritdoc/>
    public override Observable<string> Handle(string language, string key)
    {
        return handle(language, key);
    }
}

/// <summary>
/// A fake implementation of <see cref="TranslateCompiler"/> used for testing translation compilation behavior.
/// </summary>
public sealed class FakeCompiler : TranslateCompiler
{
    /// <summary>
    /// The compilation function that transforms a translation based on language and content.
    /// </summary>
    private readonly Func<string, string, string> compile;

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeCompiler"/> class.
    /// </summary>
    /// <param name="compile">
    /// The compilation function that takes a language code and translation string,
    /// and returns the compiled translation.
    /// </param>
    public FakeCompiler(Func<string, string, string> compile)
    {
        this.compile = compile;
    }

    /// <inheritdoc/>
    public override Observable<string> Compile(string language, string translation)
    {
        var compiled = compile(language, translation);
        return Observable.Return(compiled);
    }

    /// <inheritdoc/>
    public override Observable<Translations> Compile(string language, Translations translations)
    {
        var compiledDict = translations.ToDictionary(x => x.Key, x => compile(language, x.Value));
        var compiled = new Translations(compiledDict);
        return Observable.Return(compiled);
    }
}

/// <summary>
/// A fake implementation of <see cref="TranslateCache"/> used for testing translation caching behavior.
/// </summary>
public sealed class FakeCache : TranslateCache
{
    private readonly DefaultTranslateCache cache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeCache"/> class with no translations.
    /// </summary>
    public FakeCache()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeCache"/> class with a single translation.
    /// </summary>
    /// <param name="language">The language code for the translation.</param>
    /// <param name="key">The translation key.</param>
    /// <param name="translation">The translated value.</param>
    public FakeCache(string language, string key, string translation)
    {
        cache.Instant(language).Set(key, translation);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeCache"/> class with multiple languages and translations.
    /// </summary>
    /// <param name="languages">
    /// An array of tuples containing language codes and their corresponding key-translation pairs.
    /// Each tuple contains a language code and an array of key-translation tuples.
    /// </param>
    public FakeCache((string language, (string key, string translation)[])[] languages)
    {
        foreach (var (language, translations) in languages)
        {
            var l = cache.Instant(language);
            foreach (var (key, translation) in translations)
            {
                l.Set(key, translation);
            }
        }
    }

    /// <inheritdoc/>
    public override Translations Instant(string language)
    {
        return cache.Instant(language);
    }

    /// <inheritdoc/>
    public override string Instant(string language, string key)
    {
        return cache.Instant(language).Get(key);
    }

    /// <inheritdoc/>
    public override bool TryInstant(string language, string key, [NotNullWhen(true)] out string? value)
    {
        return cache.TryInstant(language, key, out value);
    }

    /// <inheritdoc/>
    public override Observable<Translations> Get(string language)
    {
        return cache.Get(language);
    }

    /// <inheritdoc/>
    public override Observable<string> Get(string language, string key)
    {
        return cache.Get(language, key);
    }

    /// <inheritdoc/>
    public override Observable<Translations> Set(string language, Translations translations, bool merge = false)
    {
        return cache.Set(language, translations, merge);
    }

    /// <inheritdoc/>
    public override Observable<string> Set(string language, string key, string value)
    {
        return cache.Set(language, key, value);
    }

    /// <inheritdoc/>
    public override Observable<R3.Unit> Remove(string language)
    {
        return cache.Remove(language);
    }

    /// <inheritdoc/>
    public override Observable<R3.Unit> Remove(string language, string key)
    {
        return cache.Remove(language, key);
    }
}
