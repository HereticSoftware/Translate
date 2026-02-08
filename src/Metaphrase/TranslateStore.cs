using Metaphrase.Primitives;
using Metaphrase.Primitives.Events;

namespace Metaphrase;

/// <summary>
/// Represents a store for managing language and translations such as Current, Fallback, Available etc.
/// </summary>
/// <remarks>Language keys are compared using <see cref="StringComparer.OrdinalIgnoreCase"/></remarks>
public sealed class TranslateStore : IDisposable
{
    private readonly Subject<LanguageChangeEvent> onFallbackLangChange = new();
    private readonly Subject<LanguageChangeEvent> onCurrentChange = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslateStore"/> class with default values.
    /// </summary>
    public TranslateStore()
    {
        Fallback = Current = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslateStore"/> class with a specified fallback language.
    /// </summary>
    /// <param name="fallbackLang">The fallback language to set.</param>
    public TranslateStore(string fallbackLang)
    {
        Fallback = Current = fallbackLang;
        Available.Add(Fallback);
    }

    /// <summary>
    /// Gets or sets the fallback language to fallback when translations are missing on the current language.
    /// </summary>
    public string Fallback
    {
        get;
        set {
            field = value;
            onCurrentChange.OnNext(new(value, Languages.Get(value)));
        }
    }

    /// <summary>
    /// Gets or sets the language currently used.
    /// </summary>
    public string Current
    {
        get;
        set {
            field = value;
            onCurrentChange.OnNext(new(value, Languages.Get(value)));
        }
    }

    /// <summary>
    /// Gets the list of available languages.
    /// </summary>
    public HashSet<string> Available { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the list of translations per language.
    /// </summary>
    public Languages Languages { get; } = new();

    /// <summary>
    /// Gets an observable to listen for fallback language change events.
    /// </summary>
    public Observable<LanguageChangeEvent> OnFallbackLangChange => onFallbackLangChange.AsObservable();

    /// <summary>
    /// Gets an observable to listen for language change events.
    /// </summary>
    public Observable<LanguageChangeEvent> OnCurrentChange => onCurrentChange.AsObservable();

    /// <summary>
    /// Gets an observable to listen for translation change events.
    /// </summary>
    public Observable<LanguageTranslationChangeEvent> OnTranslationChange => Languages.OnTranslationChange;

    /// <inheritdoc/>
    public void Dispose()
    {
        onFallbackLangChange.Dispose();
        onCurrentChange.Dispose();
    }
}
