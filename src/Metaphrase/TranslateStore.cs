using Metaphrase.Primitives;
using Metaphrase.Primitives.Events;
using Metaphrase.Primitives.Internal;

namespace Metaphrase;

/// <summary>
/// Represents a store for managing language and translations such as Current, Fallback, Available etc.
/// </summary>
/// <remarks>Language keys are compared using <see cref="StringComparer.OrdinalIgnoreCase"/></remarks>
public sealed class TranslateStore : IDisposable
{
    private readonly LazySubject<LanguageChangeEvent> onFallbackLangChange;
    private readonly LazySubject<LanguageChangeEvent> onCurrentChange;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslateStore"/> class with default values.
    /// </summary>
    /// <param name="emitChanges">A value indicating whether to emit change events. Default is <c>true</c>.</param>
    public TranslateStore(bool emitChanges = true)
    {
        Current = string.Empty;
        Fallback = string.Empty;
        onFallbackLangChange = new(emitChanges);
        onCurrentChange = new(emitChanges);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslateStore"/> class with the specified current language.
    /// </summary>
    /// <param name="current">The language to set as the current language.</param>
    /// <param name="emitChanges">A value indicating whether to emit change events. Default is <c>true</c>.</param>
    public TranslateStore(string current, bool emitChanges = true)
    {
        Current = current;
        Fallback = string.Empty;
        Available.Add(Fallback);
        onFallbackLangChange = new(emitChanges);
        onCurrentChange = new(emitChanges);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslateStore"/> class with the specified current and fallback languages.
    /// </summary>
    /// <param name="current">The language to set as the current language.</param>
    /// <param name="fallbackLang">The language to set as the fallback language.</param>
    /// <param name="emitChanges">A value indicating whether to emit change events. Default is <c>true</c>.</param>
    public TranslateStore(string current, string fallbackLang, bool emitChanges = true)
    {
        Current = current;
        Fallback = fallbackLang;
        Available.Add(Fallback);
        onFallbackLangChange = new(emitChanges);
        onCurrentChange = new(emitChanges);
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
    /// Gets or sets the fallback language to fallback when translations are missing on the current language.
    /// </summary>
    public string Fallback
    {
        get;
        set {
            field = value;
            onFallbackLangChange.OnNext(new(value, Languages.Get(value)));
        }
    }

    /// <summary>
    /// Gets the list of available languages.
    /// </summary>
    public HashSet<string> Available { get; } = [with(StringComparer.OrdinalIgnoreCase)];

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
