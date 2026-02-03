using Metaphrase.Primitives;
using Metaphrase.Primitives.Events;
using Metaphrase.Primitives.Internal;

namespace Metaphrase;

/// <summary>
/// Represents a store for managing language and translations such as Default, Current, Available etc.
/// </summary>
/// <remarks>Language keys are compared using <see cref="StringComparer.OrdinalIgnoreCase"/></remarks>
public sealed class TranslateStore : IDisposable
{
    private readonly SubjectWrapper<LanguageChangeEvent> onDefaultLangChange = new();
    private readonly SubjectWrapper<LanguageChangeEvent> onLangChange = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslateStore"/> class with default values.
    /// </summary>
    public TranslateStore()
    {
        DefaultLang = Lang = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslateStore"/> class with a specified default language.
    /// </summary>
    /// <param name="defaultLang">The default language to set.</param>
    public TranslateStore(string defaultLang)
    {
        DefaultLang = Lang = defaultLang;
        Langs.Add(DefaultLang);
    }

    /// <summary>
    /// Gets or sets the default language to fallback when translations are missing on the current language.
    /// </summary>
    public string DefaultLang
    {
        get;
        set {
            field = value;
            onLangChange.OnNext(new(value, Languages.Get(value)));
        }
    }

    /// <summary>
    /// Gets or sets the language currently used.
    /// </summary>
    public string Lang
    {
        get;
        set {
            field = value;
            onLangChange.OnNext(new(value, Languages.Get(value)));
        }
    }

    /// <summary>
    /// Gets the list of available languages.
    /// </summary>
    public HashSet<string> Langs { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the list of translations per language.
    /// </summary>
    public Languages Languages { get; } = new();

    /// <summary>
    /// Gets an observable to listen for default language change events.
    /// </summary>
    public Observable<LanguageChangeEvent> OnDefaultLangChange => onDefaultLangChange.GetObservable();

    /// <summary>
    /// Gets an observable to listen for language change events.
    /// </summary>
    public Observable<LanguageChangeEvent> OnLangChange => onLangChange.GetObservable();

    /// <summary>
    /// Gets an observable to listen for translation change events.
    /// </summary>
    public Observable<LanguageTranslationChangeEvent> OnTranslationChange => Languages.OnTranslationChange;

    /// <inheritdoc/>
    public void Dispose()
    {
        onDefaultLangChange.Dispose();
        onLangChange.Dispose();
    }
}
