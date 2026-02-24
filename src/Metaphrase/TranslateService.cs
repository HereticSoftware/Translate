using Metaphrase.Abstract;
using Metaphrase.Defaults;
using Metaphrase.Primitives;
using Metaphrase.Primitives.Events;
using Metaphrase.Primitives.Internal;

namespace Metaphrase;

/// <summary>
/// Provides translation services, including loading, setting, and retrieving translations for different languages.
/// </summary>
public sealed class TranslateService
{
    private readonly LazySubject<LanguageChangeEvent> onFallbackLangChange;
    private readonly LazySubject<LanguageChangeEvent> onCurrentChange;

    private readonly TranslateCache cache;
    private readonly TranslateLoader loader;
    private readonly TranslateMissingHandler missingHandler;
    private readonly TranslateCompiler compiler;
    private readonly TranslateParser parser;
    private readonly TranslateServiceOptions options;

    private readonly ConcurrentLazyDictionary<string, TranslationLoader> translationLoaders = [];

    /// <summary>
    /// Gets or sets the language currently used.
    /// </summary>
    public string Current
    {
        get;
        set {
            if (field == value) return;
            field = value;
            onCurrentChange.OnNext(new(value));
        }
    }

    /// <summary>
    /// Gets or sets the fallback language to fallback when translations are missing on the current language.
    /// </summary>
    /// <remarks>Empty string disables the fallback.</remarks>
    public string Fallback
    {
        get;
        set {
            if (field == value) return;
            field = value;
            onFallbackLangChange.OnNext(new(value));
        }
    }

    /// <summary>
    /// Gets the list of available languages.
    /// </summary>
    /// <remarks>The set is created using <see cref="StringComparer.OrdinalIgnoreCase"/>.</remarks>
    public HashSet<string> Available { get; }

    /// <summary>
    /// An observable to listen to fallback language change events.
    /// </summary>
    public Observable<LanguageChangeEvent> OnFallbackLangChange => onFallbackLangChange.AsObservable();

    /// <summary>
    /// An observable to listen to language change events.
    /// </summary>
    public Observable<LanguageChangeEvent> OnCurrentChange => onCurrentChange.AsObservable();

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslateService"/> class.
    /// </summary>
    /// <param name="parser">An instance of the parser to use.</param>
    /// <param name="loader">An instance of the loader to use.</param>
    /// <param name="missingHandler">An instance of the missing translation handler to use.</param>
    /// <param name="compiler">An instance of the compiler to use.</param>
    /// <param name="cache">An instance of the cache to use.</param>
    /// <param name="options">Options to configure the service.</param>
    public TranslateService(
        TranslateParser? parser = null,
        TranslateLoader? loader = null,
        TranslateMissingHandler? missingHandler = null,
        TranslateCompiler? compiler = null,
        TranslateCache? cache = null,
        TranslateServiceOptions? options = null)
    {
        this.parser = parser ?? DefaultTranslateParser.Instance;
        this.loader = loader ?? DefaultTranslateLoader.Instance;
        this.missingHandler = missingHandler ?? DefaultTranslateMissingHandler.Instance;
        this.compiler = compiler ?? DefaultTranslateCompiler.Instance;
        this.cache = cache ?? new DefaultTranslateCache();
        this.options = options ?? new();

        Current = this.options.Current;
        Fallback = this.options.Fallback;
        Available = [with(StringComparer.OrdinalIgnoreCase), .. this.options.Available];

        onFallbackLangChange = new(this.options.EmitChanges);
        onCurrentChange = new(this.options.EmitChanges);
    }

    /// <summary>
    /// Adds available languages.
    /// </summary>
    /// <param name="langs">The languages to add.</param>
    public void AddAvailable(params ReadOnlySpan<string> langs)
    {
        foreach (string lang in langs)
        {
            Available.Add(lang);
        }
    }

    /// <summary>
    /// Load translations for a given language if they have not been loaded yet.
    /// </summary>
    /// <param name="language">The language to load.</param>
    /// <remarks>
    /// If there is already a loading request or the language has already been loaded it will be returned.
    /// You can call <see cref="Reset(string)"/> to cancel it and load again or <see cref="Reload(string, bool)"/> that does exactly that.
    /// </remarks>
    /// <returns>An observable sequence of translations for the specified language.</returns>
    public Observable<Translations> Load(string language)
    {
        if (string.IsNullOrEmpty(language))
            return Observable.Empty<Translations>();

        return translationLoaders.GetOrAdd(language, lang => new TranslationLoader(this, lang)).Load;
    }

    /// <summary>
    /// Load the current language translations if they have not been loaded yet.
    /// </summary>
    /// <returns>An observable sequence of translations for the current language.</returns>
    public Observable<Translations> LoadCurrent()
    {
        return Load(Current); ;
    }

    /// <summary>
    /// Set the current language and load it's translations if they have not been loaded yet.
    /// </summary>
    /// <returns>An observable sequence of translations for the current language.</returns>
    public Observable<Translations> LoadAndSetCurrent(string current)
    {
        Current = current;
        return LoadCurrent();
    }

    /// <summary>
    /// Load the fallback language translations if they have not been loaded yet.
    /// </summary>
    /// <returns>An observable sequence of translations for the fallback language.</returns>
    public Observable<Translations> LoadFallback()
    {
        return Load(Fallback);
    }

    /// <summary>
    /// Set the fallback language and load it's translations if they have not been loaded yet.
    /// </summary>
    /// <returns>An observable sequence of translations for the fallback language.</returns>
    public Observable<Translations> LoadAndSetFallback(string fallback)
    {
        Fallback = fallback;
        return LoadFallback();
    }

    /// <summary>
    /// Deletes translations for the provided language.
    /// </summary>
    /// <param name="language">The language key to reset.</param>
    public Observable<Unit> Reset(string language)
    {
        if (translationLoaders.TryRemove(language, out var loader) && loader.IsValueCreated)
        {
            loader.Value.Dispose();
        }
        return cache.Remove(language);
    }

    /// <summary>
    /// Reloads the provided language by calling <see cref="Reset(string)"/> and then <see cref="Load(string)"/>.
    /// </summary>
    /// <param name="lang">The language to reload.</param>
    /// <returns>An observable sequence of translations for the reloaded language.</returns>
    public Observable<Translations> Reload(string lang)
    {
        Reset(lang);
        return Load(lang);
    }

    /// <summary>
    /// Gets the translations of a language.
    /// </summary>
    /// <param name="language">The language key.</param>
    /// <returns>An observable sequence of the translations.</returns>
    public Translations InstantTranslations(string language)
    {
        return cache.Instant(language);
    }

    /// <summary>
    /// Returns a translation instantly from the internal state of loaded translations.
    /// If Current is not available Fallback is attepmted (if configured). Otherwise the key is returned as is.
    /// </summary>
    /// <param name="key">The key of the translation.</param>
    /// <param name="parameters">The parameters to use for parsing.</param>
    /// <returns>A <see cref="TranslateString"/> containing the translated value.</returns>
    public TranslateString Instant(string key, object? parameters = null)
    {
        if (cache.TryInstant(Current, key, out var value))
        {
            return new TranslateString(value, parameters, parser);
        }
        else if (!string.IsNullOrEmpty(Fallback) && cache.TryInstant(Fallback, key, out value))
        {
            return new TranslateString(value, parameters, parser);
        }
        return new TranslateString(key, parameters, parser);
    }

    /// <summary>
    /// Returns a translation instantly from the internal state of loaded translations.
    /// </summary>
    /// <param name="lang">The language to use for translation.</param>
    /// <param name="key">The key of the translation.</param>
    /// <param name="parameters">The parameters to use for parsing.</param>
    /// <returns>A <see cref="TranslateString"/> containing the translated value.</returns>
    public TranslateString Instant(string lang, string key, object? parameters = null)
    {
        return cache.TryInstant(lang, key, out var value)
            ? new TranslateString(value, parameters, parser)
            : new TranslateString(key, parameters, parser);
    }

    /// <summary>
    /// Gets the translations of a language.
    /// </summary>
    /// <param name="language">The language key.</param>
    /// <returns>An observable sequence of the translations.</returns>
    public Observable<Translations> GetTranslations(string language)
    {
        return Observable
            .Defer((translationLoaders, language), static s => s.translationLoaders.TryGet(s.language, out var loader) ? loader.Load : Observable.Return(new Translations()))
            .Select((cache, language), static (_, s) => s.cache.Get(s.language))
            .SelectMany(static v => v);
    }

    /// <summary>
    /// Gets the translated value of a key.
    /// </summary>
    /// <param name="key">The key of the translation.</param>
    /// <param name="parameters">The parameters to use for parsing.</param>
    /// <returns>An observable sequence of the translated value.</returns>
    public Observable<string> Get(string key, object? parameters = null)
    {
        return Get(Current, key, parameters)
            .SelectIf(
                state: (service: this, key, parameters),
                condition: (value, s) => value == s.key && !string.IsNullOrEmpty(s.service.Fallback),
                whenTrue: (value, s) => s.service.Get(s.service.Fallback, s.key, s.parameters),
                whenFasle: (value, _) => Observable.Return(value)
            )
            .SelectMany(static v => v);
    }

    /// <summary>
    /// Gets the translated value of a key.
    /// </summary>
    /// <param name="language">The language to use for translation.</param>
    /// <param name="key">The key of the translation.</param>
    /// <param name="parameters">The parameters to use during parsing.</param>
    /// <returns>An observable sequence of the translated value.</returns>
    public Observable<string> Get(string language, string key, object? parameters = null)
    {
        return Observable
            .Defer((translationLoaders, language), static s => s.translationLoaders.TryGet(s.language, out var loader) ? loader.Load : Observable.Return(new Translations()))
            .Select((cache, language, key), static (_, s) => s.cache.Get(s.language, s.key))
            .SelectMany(static v => v)
            .SelectIf(
                state: (service: this, language, key),
                condition: (value, s) => value != s.key || s.service.missingHandler == DefaultTranslateMissingHandler.Instance,
                whenTrue: (value, _) => Observable.Return(value),
                whenFasle: (value, s) => s.service.missingHandler
                    .Handle(s.language, s.key)
                    .SelectIf(
                        state: (service: this, language, key),
                        condition: (value, s) => string.IsNullOrEmpty(value) || value == s.key,
                        whenTrue: (value, s) => Observable.Return(s.key),
                        whenFasle: (value, s) => s.service.Set(s.language, s.key, value).Select(value, (_, v) => v)
                    )
                    .SelectMany(static v => v)
            )
            .SelectMany(static v => v)
            .Select((parameters, parser), static (value, s) => new TranslateString(value, s.parameters, s.parser).ToString());
    }

    /// <summary>
    /// Sets the translations of a specific language, after compiling it.
    /// </summary>
    /// <param name="language">The language for which to set the translations.</param>
    /// <param name="translations">The translations to set.</param>
    /// <param name="merge">Whether to merge the new translations with existing translations.</param>
    public Observable<Translations> SetTranslations(string language, Translations translations, bool merge = false)
    {
        return Observable
            .Defer(static () => Observable.Return(Unit.Default))
            // compile
            .Select((compiler, language, translations), static (_, s) => s.compiler.Compile(s.language, s.translations))
            .SelectMany(static t => t)
            // cache
            .Select((cache, language, merge), static (translations, s) => s.cache.Set(s.language, translations, s.merge))
            .SelectMany(static x => x);
    }

    /// <summary>
    /// Sets the translation of a key for a specific language, after compiling it.
    /// </summary>
    /// <param name="language">The language for which to set the translation.</param>
    /// <param name="key">The key of the translation to set.</param>
    /// <param name="value">The translation value to compile and store.</param>
    public Observable<string> Set(string language, string key, string value)
    {
        return Observable
            .Defer(static () => Observable.Return(Unit.Default))
            // compile
            .Select((compiler, language, value), static (_, s) => s.compiler.Compile(s.language, s.value))
            .SelectMany(static t => t)
            // cache
            .Select((cache, language, key), static (value, s) => s.cache.Set(s.language, s.key, value))
            .SelectMany(static x => x);
    }

    /// <summary>
    /// Defines a bitwise OR operator for translating a string using a specified translation service.
    /// </summary>
    /// <param name="key">The string to be translated based on the current language setting.</param>
    /// <param name="service">The translation service that provides access to language resources and parsing functionality.</param>
    /// <returns>Returns the translated string based on the provided key and current language.</returns>
    public static TranslateString operator |(string key, TranslateService service)
    {
        return service.Instant(key, parameters: null);
    }

    /// <summary>
    /// Defines a bitwise OR operator for translating a string using a specified translation service.
    /// </summary>
    /// <param name="service">The translation service that provides access to language resources and parsing functionality.</param>
    /// <param name="key">The string to be translated based on the current language setting.</param>
    /// <returns>Returns the translated string based on the provided key and current language.</returns>
    public static TranslateString operator |(TranslateService service, string key)
    {
        return service.Instant(key, parameters: null);
    }

    /// <summary>
    /// Represents a loader for translations with cancellation support.
    /// </summary>
    private readonly struct TranslationLoader : IDisposable
    {
        private readonly CancellationTokenSource cts = new();

        /// <summary>
        /// Gets an observable sequence of translations for the specified language.
        /// </summary>
        public Observable<Translations> Load { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslationLoader"/> struct.
        /// </summary>
        /// <param name="service">The translation service that provides access to language resources and parsing functionality.</param>
        /// <param name="language">The language code for which to load translations.</param>
        /// <summary>
        /// Initializes a new instance of the <see cref="TranslationLoader"/> struct.
        /// </summary>
        /// <param name="language">The language code for which to load translations.</param>
        public TranslationLoader(TranslateService service, string language)
        {
            Load = Observable
                .Defer(static () => Observable.Return(Unit.Default))
                // load
                .Select((service, language), static (_, s) => s.service.loader.GetTranslation(s.language))
                .SelectMany(static t => t)
                // set
                .Select((service, language), static (translations, s) => s.service.SetTranslations(s.language, translations).Select(translations, static (_, t) => t))
                .SelectMany(static t => t)
                //.Do(cts, static (_, cts) => cts.Cancel())
                // cancel
                .TakeUntil(cts.Token)
                // ensure all receive the same observable
                .Replay()
                .RefCount();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            cts.Cancel();
            cts.Dispose();
        }
    }
}
