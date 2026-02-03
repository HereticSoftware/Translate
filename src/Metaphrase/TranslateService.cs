using Metaphrase.Abstract;
using Metaphrase.Defaults;
using Metaphrase.Primitives;
using Metaphrase.Primitives.Events;

namespace Metaphrase;

/// <summary>
/// Provides translation services, including loading, setting, and retrieving translations for different languages.
/// </summary>
public sealed class TranslateService
{
    private readonly TranslateStore store;
    private readonly TranslateLoader loader;
    private readonly TranslateParser parser;
    private readonly TranslateCompiler compiler;
    private readonly TranslateServiceOptions options;

    private readonly ConcurrentDictionary<string, TranslationLoader> translationsLoading = [];

    /// <summary>
    /// The default language to fallback when translations are missing in the current language.
    /// </summary>
    public string DefaultLang => store.DefaultLang;

    /// <summary>
    /// The language currently used.
    /// </summary>
    public string CurrentLang => store.Lang;

    /// <summary>
    /// A list of translations per language.
    /// </summary>
    public Languages Languages => store.Languages;

    /// <summary>
    /// A list of available languages.
    /// </summary>
    public HashSet<string> Langs => store.Langs;

    /// <summary>
    /// An observable to listen to default language change events.
    /// </summary>
    public Observable<LanguageChangeEvent> OnDefaultLangChange => store.OnDefaultLangChange;

    /// <summary>
    /// An observable to listen to language change events.
    /// </summary>
    public Observable<LanguageChangeEvent> OnLangChange => store.OnLangChange;

    /// <summary>
    /// An observable to listen to translation change events.
    /// </summary>
    public Observable<LanguageTranslationChangeEvent> OnTranslationChange => store.OnTranslationChange;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslateService"/> class.
    /// </summary>
    /// <param name="store">An instance of the store (that is supposed to be unique).</param>
    /// <param name="loader">An instance of the loader to use.</param>
    /// <param name="parser">An instance of the parser currently used.</param>
    /// <param name="compiler">An instance of the compiler currently used.</param>
    /// <param name="options">Options to configure the current service.</param>
    public TranslateService(
        TranslateStore? store = null,
        TranslateLoader? loader = null,
        TranslateParser? parser = null,
        TranslateCompiler? compiler = null,
        TranslateServiceOptions? options = null)
    {
        this.store = store ?? new();
        this.loader = loader ?? DefaultTranslateLoader.Instance;
        this.parser = parser ?? DefaultTranslateParser.Instance;
        this.compiler = compiler ?? DefaultTranslateCompiler.Instance;
        this.options = options ?? new();

        if (!string.IsNullOrEmpty(this.options.DefaultLanguage))
        {
            this.store.DefaultLang = this.options.DefaultLanguage;
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
        if (lang == DefaultLang && store.Languages.TryGet(lang, out Translations? current))
        {
            return Observable.Return(current);
        }
        // we already have this language
        else if (store.Languages.TryGet(lang, out Translations? translations))
        {
            ChangeDefaultLang(lang);
            return Observable.Return(translations);
        }
        // load the new language
        ChangeDefaultLang(lang);
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
        if (lang == CurrentLang && store.Languages.TryGet(lang, out Translations? current))
        {
            return Observable.Return(current);
        }
        // we already have this language
        else if (store.Languages.TryGet(lang, out Translations? translations))
        {
            ChangeLang(lang);
            return Observable.Return(translations);
        }
        // load the new language
        ChangeLang(lang);
        return LoadTranslation(lang);
    }

    /// <summary>
    /// Gets translations for a given language with the current loader.
    /// </summary>
    /// <param name="lang">The language to load.</param>
    /// <param name="merge">Whether to merge with the current translations or replace them.</param>
    /// <remarks>
    /// If there is already a loading request it will be returned.
    /// You can call <see cref="ResetLang(string)"/> to cancel it and load again.
    /// </remarks>
    /// <returns>An observable sequence of translations for the specified language.</returns>
    public Observable<Translations> LoadTranslation(string lang, bool merge = false)
    {
        return translationsLoading.GetOrAdd(lang, lang => new TranslationLoader(this, lang, merge)).Load;
    }

    /// <summary>
    /// Adds available languages.
    /// </summary>
    /// <param name="langs">The languages to add.</param>
    public void AddLangs(params ReadOnlySpan<string> langs)
    {
        foreach (string lang in langs)
        {
            Langs.Add(lang);
        }
    }

    /// <summary>
    /// Changes the current language.
    /// </summary>
    /// <param name="lang">The language to set as current.</param>
    private void ChangeLang(string lang)
    {
        store.Lang = lang;
        // if there is no default language, use the one that we just set
        if (string.IsNullOrEmpty(DefaultLang))
        {
            ChangeDefaultLang(lang);
        }
    }

    /// <summary>
    /// Changes the default language.
    /// </summary>
    /// <param name="lang">The language to set as default.</param>
    private void ChangeDefaultLang(string lang)
    {
        store.DefaultLang = lang;
        // if there is no current language, use the one that we just set
        if (string.IsNullOrEmpty(CurrentLang))
        {
            ChangeLang(lang);
        }
    }

    /// <summary>
    /// Reloads the provided language.
    /// </summary>
    /// <param name="lang">The language to reload.</param>
    /// <param name="merge">Whether to merge with the current translations or replace them.</param>
    /// <returns>An observable sequence of translations for the reloaded language.</returns>
    public Observable<Translations> ReloadLang(string lang, bool merge = false)
    {
        ResetLang(lang);
        return LoadTranslation(lang, merge);
    }

    /// <summary>
    /// Deletes inner translations for the provided language.
    /// </summary>
    /// <param name="lang">The language key to reset.</param>
    public void ResetLang(string lang)
    {
        if (translationsLoading.TryRemove(lang, out var loader))
        {
            loader.Cancel();
        }
        store.Languages.Remove(lang);
    }

    /// <summary>
    /// Returns a translation instantly from the internal state of loaded translations.
    /// All rules regarding the current language, the preferred language, or even fallback languages will be used except any promise handling.
    /// </summary>
    /// <param name="key">The key of the translation.</param>
    /// <param name="parameters">The parameters to use for parsing.</param>
    /// <returns>A <see cref="TranslateString"/> containing the translated value.</returns>
    public TranslateString Instant(string key, object? parameters)
    {
        // todo: If not found return default depending on options
        // todo: Implement missing handler
        return Instant(CurrentLang, key, parameters);
    }

    /// <summary>
    /// Gets the translated value of a key.
    /// </summary>
    /// <param name="key">The key of the translation.</param>
    /// <param name="parameters">The parameters to use for parsing.</param>
    /// <returns>An observable sequence of the translated value.</returns>
    public Observable<string> Get(string key, object? parameters = null)
    {
        // todo: If not found return default depending on options
        // todo: Implement missing handler
        return Get(CurrentLang, key, parameters);
    }

    /// <summary>
    /// Returns a translation instantly from the internal state of loaded translations.
    /// All rules regarding the current language, the preferred language, or even fallback languages will be used except any promise handling.
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

    // todo: Add the GetMany method.
    // todo: Consider adding the Set and SetMany methods.

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
    private readonly struct TranslationLoader
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
                .Do((service, lang, merge), static (translations, state) =>
                {
                    var (service, lang, merge) = state;
                    service.Langs.Add(lang);
                    translations = service.compiler.CompileTranslations(translations, lang);
                    if (merge)
                        service.store.Languages.Get(lang).Merge(translations);
                    else
                        service.store.Languages.Set(lang, translations);
                })
                .Replay()
                .RefCount()
                .TakeUntil(cts.Token);
        }

        /// <summary>
        /// Cancels the loading of translations.
        /// </summary>
        public readonly void Cancel()
        {
            cts.Cancel();
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
