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
    private readonly TranslateStore store;
    private readonly TranslateLoader loader;
    private readonly TranslateCompiler compiler;
    private readonly TranslateParser parser;
    private readonly TranslateServiceOptions options;
#if NET9_0_OR_GREATER
    private readonly Lock setGate = new();
#else
    private readonly object setGate = new();
#endif

    private readonly ConcurrentLazyDictionary<string, TranslationLoader> translationLoaders = [];

    /// <summary>
    /// The default language to fallback when translations are missing in the current language.
    /// </summary>
    public string Fallback => store.Fallback;

    /// <summary>
    /// The language currently used.
    /// </summary>
    public string Current => store.Current;

    /// <summary>
    /// A list of available languages.
    /// </summary>
    public HashSet<string> Available => store.Available;

    /// <summary>
    /// A list of translations per language.
    /// </summary>
    public Languages Languages => store.Languages;

    /// <summary>
    /// An observable to listen to fallback language change events.
    /// </summary>
    public Observable<LanguageChangeEvent> OnFallbackLangChange => store.OnFallbackLangChange;

    /// <summary>
    /// An observable to listen to language change events.
    /// </summary>
    public Observable<LanguageChangeEvent> OnCurrentChange => store.OnCurrentChange;

    /// <summary>
    /// An observable to listen to translation change events.
    /// </summary>
    public Observable<LanguageTranslationChangeEvent> OnTranslationChange => store.OnTranslationChange;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslateService"/> class.
    /// </summary>
    /// <param name="store">An instance of the store (that is supposed to be unique).</param>
    /// <param name="loader">An instance of the loader to use.</param>
    /// <param name="compiler">An instance of the compiler currently used.</param>
    /// <param name="parser">An instance of the parser currently used.</param>
    /// <param name="options">Options to configure the current service.</param>
    public TranslateService(
        TranslateStore? store = null,
        TranslateLoader? loader = null,
        TranslateCompiler? compiler = null,
        TranslateParser? parser = null,
        TranslateServiceOptions? options = null)
    {
        this.store = store ?? new();
        this.loader = loader ?? DefaultTranslateLoader.Instance;
        this.compiler = compiler ?? DefaultTranslateCompiler.Instance;
        this.parser = parser ?? DefaultTranslateParser.Instance;
        this.options = options ?? new();

        if (!string.IsNullOrEmpty(this.options.DefaultLanguage))
        {
            this.store.Fallback = this.options.DefaultLanguage;
        }
    }

    /// <summary>
    /// Sets the default language to use as a fallback.
    /// </summary>
    /// <param name="lang">The language to set as default.</param>
    /// <returns>An observable sequence of translations for the default language.</returns>
    public Observable<Translations> SetDefaultLang(string lang)
    {
        // Default is equal to requested and we already have it
        if (lang == Fallback && store.Languages.TryGet(lang, out Translations? current))
        {
            return Observable.Return(current);
        }
        // we already have this language
        else if (store.Languages.TryGet(lang, out Translations? translations))
        {
            ChangeFallback(lang);
            return Observable.Return(translations);
        }
        // load the new language
        ChangeFallback(lang);
        return LoadTranslation(lang);
    }

    /// <summary>
    /// Sets the language to use.
    /// </summary>
    /// <param name="lang">The language to set as current.</param>
    /// <returns>An observable sequence of translations for the current language.</returns>
    public Observable<Translations> SetCurrentLang(string lang)
    {
        // Current is equal to requested and we already have it
        if (lang == Current && store.Languages.TryGet(lang, out Translations? current))
        {
            return Observable.Return(current);
        }
        // we already have this language
        else if (store.Languages.TryGet(lang, out Translations? translations))
        {
            ChangeCurrent(lang);
            return Observable.Return(translations);
        }
        // load the new language
        ChangeCurrent(lang);
        return LoadTranslation(lang);
    }

    /// <summary>
    /// Gets translations for a given language with the current loader.
    /// </summary>
    /// <param name="lang">The language to load.</param>
    /// <param name="merge">Whether to merge with the current translations or replace them.</param>
    /// <remarks>
    /// If there is already a loading request it will be returned.
    /// You can call <see cref="Reset(string)"/> to cancel it and load again.
    /// </remarks>
    /// <returns>An observable sequence of translations for the specified language.</returns>
    public Observable<Translations> LoadTranslation(string lang, bool merge = false)
    {
        return translationLoaders.GetOrAdd(lang, lang => new TranslationLoader(this, lang, merge)).Load;
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
    /// Changes the current language.
    /// </summary>
    /// <param name="lang">The language to set as current.</param>
    private void ChangeCurrent(string lang)
    {
        store.Current = lang;
        // if there is no default language, use the one that we just set
        if (string.IsNullOrEmpty(Fallback))
        {
            ChangeFallback(lang);
        }
    }

    /// <summary>
    /// Changes the fallback language.
    /// </summary>
    /// <param name="lang">The language to set as fallback.</param>
    private void ChangeFallback(string lang)
    {
        store.Fallback = lang;
        // if there is no current language, use the one that we just set
        if (string.IsNullOrEmpty(Current))
        {
            ChangeCurrent(lang);
        }
    }

    /// <summary>
    /// Reloads the provided language.
    /// </summary>
    /// <param name="lang">The language to reload.</param>
    /// <param name="merge">Whether to merge with the current translations or replace them.</param>
    /// <returns>An observable sequence of translations for the reloaded language.</returns>
    public Observable<Translations> Reload(string lang, bool merge = false)
    {
        Reset(lang);
        return LoadTranslation(lang, merge);
    }

    /// <summary>
    /// Deletes inner translations for the provided language.
    /// </summary>
    /// <param name="lang">The language key to reset.</param>
    public void Reset(string lang)
    {
        if (translationLoaders.TryRemove(lang, out var loader) && loader.IsValueCreated)
        {
            loader.Value.Dispose();
        }
        store.Languages.Remove(lang);
    }

    /// <summary>
    /// Returns a translation instantly from the internal state of loaded translations.
    /// If Current is not available Fallback is attepmted. Otherwise the key is returned as is.
    /// </summary>
    /// <param name="key">The key of the translation.</param>
    /// <param name="parameters">The parameters to use for parsing.</param>
    /// <returns>A <see cref="TranslateString"/> containing the translated value.</returns>
    public TranslateString Instant(string key, object? parameters)
    {
        if (store.Languages.TryGet(Current, out var translations) && translations.TryGetParsedResult(key, parameters, parser, out var translateString))
        {
            return translateString;
        }
        else if (store.Languages.TryGet(Fallback, out translations) && translations.TryGetParsedResult(key, parameters, parser, out translateString))
        {
            return translateString;
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
    public TranslateString Instant(string lang, string key, object? parameters)
    {
        return store.Languages.TryGet(lang, out var translations)
            ? translations.GetParsedResult(key, parameters, parser)
            : new TranslateString(key, parameters, parser);
    }

    /// <summary>
    /// Gets the translated value of a key.
    /// </summary>
    /// <remarks>If the language is not available it will be loaded.</remarks>
    /// <param name="key">The key of the translation.</param>
    /// <param name="parameters">The parameters to use for parsing.</param>
    /// <returns>An observable sequence of the translated value.</returns>
    public Observable<string> Get(string key, object? parameters = null)
    {
        return Get(Current, key, parameters);
    }

    /// <summary>
    /// Gets the translated value of a key.
    /// </summary>
    /// <remarks>If the language is not available it will be loaded.</remarks>
    /// <param name="lang">The language to use for translation.</param>
    /// <param name="key">The key of the translation.</param>
    /// <param name="parameters">The parameters to use for parsing.</param>
    /// <returns>An observable sequence of the translated value.</returns>
    public Observable<string> Get(string lang, string key, object? parameters = null)
    {
        var translations = store.Languages.TryGet(lang, out Translations? value)
            ? Observable.Return(value)
            : LoadTranslation(lang);

        return translations.Select(t => t.GetParsedResult(key, parameters, parser).ToString());
    }

    /// <summary>
    /// Sets the translated value of a key for a specific language, after compiling it.
    /// </summary>
    /// <param name="lang">The language for which to set the translation.</param>
    /// <param name="key">The key of the translation to set.</param>
    /// <param name="value">The translation value to compile and store.</param>
    public void Set(string lang, string key, string value)
    {
        var translations = store.Languages.Get(lang);
        lock (setGate)
        {
            translations[key] = compiler.Compile(value, lang);
        }
    }

    /// <summary>
    /// Defines a bitwise OR operator for translating a string using a specified translation service.
    /// </summary>
    /// <param name="key">The string to be translated based on the current language setting.</param>
    /// <param name="service">The translation service that provides access to language resources and parsing functionality.</param>
    /// <returns>Returns the translated string based on the provided key and current language.</returns>
    public static TranslateString operator |(string key, TranslateService service)
    {
        return service.Instant(key, null);
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
        /// <param name="lang">The language code for which to load translations.</param>
        /// <param name="merge">A value indicating whether to merge the new translations with existing ones or replace them.</param>
        /// <summary>
        /// Initializes a new instance of the <see cref="TranslationLoader"/> struct.
        /// </summary>
        /// <param name="lang">The language code for which to load translations.</param>
        public TranslationLoader(TranslateService service, string lang, bool merge)
        {
            Load = CreateDefer((service, lang), static state => state.service.loader.GetTranslation(state.lang))
                .TakeUntil(cts.Token)
                .Do((service, lang, merge), static (translations, state) =>
                {
                    var (service, lang, merge) = state;
                    service.Available.Add(lang);
                    translations = service.compiler.CompileTranslations(translations, lang);
                    if (merge)
                        service.store.Languages.Get(lang).Merge(translations);
                    else
                        service.store.Languages.Set(lang, translations);
                })
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

    internal static Defer<T, TState> CreateDefer<T, TState>(TState state, Func<TState, Observable<T>> observableFactory, bool rawObserver = false)
    {
        return new Defer<T, TState>(state, observableFactory, rawObserver);
    }

    internal sealed class Defer<T, TState>(TState state, Func<TState, Observable<T>> observableFactory, bool rawObserver) : Observable<T>
    {
        protected override IDisposable SubscribeCore(Observer<T> observer)
        {
            var observable = default(Observable<T>);
            try
            {
                observable = observableFactory(state);
            }
            catch (Exception ex)
            {
                observer.OnCompleted(ex); // when failed, return Completed(Error)
                return Disposable.Empty;
            }

            return observable.Subscribe(rawObserver ? observer : observer.Wrap());
        }
    }
}
