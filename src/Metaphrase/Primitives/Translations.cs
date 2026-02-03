using Metaphrase.Abstract;
using Metaphrase.Primitives.Events;
using System.Collections;
using System.Text.Json;

namespace Metaphrase.Primitives;

/// <summary>
/// Represents a collection of translations.
/// </summary>
/// <remarks>Language keys are compared using <see cref="StringComparer.Ordinal"/></remarks>
public sealed class Translations : IEnumerable<KeyValuePair<string, string>>, IDisposable
{
    private readonly Subject<TranslationChangeEvent> onTranslationChange = new();
    private readonly Dictionary<string, string> store;

    /// <summary>
    /// Gets an observable that notifies when a translation changes.
    /// </summary>
    public Observable<TranslationChangeEvent> OnTranslationChange => onTranslationChange.AsObservable();

    /// <summary>
    /// Initializes a new instance of the <see cref="Translations"/> class.
    /// </summary>
    public Translations()
    {
        store = new(StringComparer.Ordinal);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Translations"/> class with the specified translations.
    /// </summary>
    /// <param name="translations">The initial translations.</param>
    public Translations(IDictionary<string, string> translations)
    {
        store = new(translations, StringComparer.Ordinal);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Translations"/> class with the specified capacity.
    /// </summary>
    /// <param name="capacity">The initial capacity of the translations store.</param>
    public Translations(int capacity)
    {
        store = new(capacity, StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets or sets the translation for the specified key.
    /// </summary>
    /// <param name="key">The key of the translation.</param>
    /// <returns>The translation if found; otherwise, the key.</returns>
    public string this[string key]
    {
        get => store.GetValueOrDefault(key, key);
        set {
            store[key] = value;
            onTranslationChange.OnNext(new(key, value));
        }
    }

    /// <summary>
    /// Merges the specified dictionary of values into the current translations.
    /// </summary>
    /// <param name="values">The dictionary of values to merge.</param>
    public void Merge(Translations translations)
    {
        foreach (var (k, v) in translations.store)
        {
            store[k] = v;
        }
        onTranslationChange.OnNext(TranslationChangeEvent.Multiple);
    }

    /// <summary>
    /// Merges the specified dictionary of values into the current translations.
    /// </summary>
    /// <param name="values">The dictionary of values to merge.</param>
    public void Merge(IDictionary<string, string> values)
    {
        foreach (var (k, v) in values)
        {
            store[k] = v;
        }
        onTranslationChange.OnNext(TranslationChangeEvent.Multiple);
    }

    /// <summary>
    /// Merges the specified dictionary of values into the current translations.
    /// </summary>
    /// <param name="values">The dictionary of values to merge.</param>
    public void Merge(IEnumerable<KeyValuePair<string, string>> values)
    {
        foreach (var (k, v) in values)
        {
            store[k] = v;
        }
        onTranslationChange.OnNext(TranslationChangeEvent.Multiple);
    }

    /// <summary>
    /// Gets the parsed result for the specified key using the provided parameters and parser.
    /// </summary>
    /// <param name="key">The key of the translation.</param>
    /// <param name="parameters">The parameters to use for parsing.</param>
    /// <param name="parser">The parser to use for parsing.</param>
    /// <returns>A <see cref="TranslateString"/> containing the parsed result.</returns>
    public TranslateString GetParsedResult(string key, object? parameters, TranslateParser? parser)
    {
        return new(this[key], parameters, parser);
    }

    /// <summary>
    /// Creates a <see cref="Translations"/> object from a <see cref="JsonDocument"/>.
    /// </summary>
    /// <param name="json">The JSON document to parse.</param>
    /// <returns>A <see cref="Translations"/> object populated with the parsed data.</returns>
    public static Translations FromJson(JsonDocument? json)
    {
        return FromJson(json?.RootElement);
    }

    /// <summary>
    /// Creates a <see cref="Translations"/> object from a <see cref="JsonElement"/>.
    /// </summary>
    /// <param name="json">The JSON document to parse.</param>
    /// <returns>A <see cref="Translations"/> object populated with the parsed data.</returns>
    public static Translations FromJson(in JsonElement? json)
    {
        var translations = new Translations();

        return json?.ValueKind switch
        {
            JsonValueKind.Object => FromObject(translations, string.Empty, json.Value),
            JsonValueKind.Array => FromArray(translations, string.Empty, json.Value),
            _ => translations
        };

        static Translations FromObject(Translations translations, string parentName, in JsonElement element)
        {
            foreach (var obj in element.EnumerateObject())
            {
                var name = $"{parentName}{obj.Name}";
                if (obj.Value.ValueKind is JsonValueKind.Object)
                {
                    FromObject(translations, $"{name}.", obj.Value);
                }
                else if (obj.Value.ValueKind is JsonValueKind.Array)
                {
                    FromArray(translations, $"{name}.", obj.Value);
                }
                else if (obj.Value.ValueKind is not (JsonValueKind.Undefined or JsonValueKind.Null))
                {
                    translations[name] = obj.Value.ToString();
                }
            }
            return translations;
        }

        static Translations FromArray(Translations translations, string parentName, in JsonElement element)
        {
            var index = 0;
            foreach (var obj in element.EnumerateArray())
            {
                var name = $"{parentName}{index}";
                if (obj.ValueKind is JsonValueKind.Object)
                {
                    FromObject(translations, $"{name}.", obj);
                }
                else if (obj.ValueKind is JsonValueKind.Array)
                {
                    FromArray(translations, $"{name}.", obj);
                }
                else if (obj.ValueKind is not (JsonValueKind.Undefined or JsonValueKind.Null))
                {
                    translations[name] = obj.ToString();
                }
                index++;
            }
            return translations;
        }
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        return store.GetEnumerator();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        onTranslationChange.Dispose();
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
