using System.Collections;
using System.Text.Json;

namespace Metaphrase.Primitives;

/// <summary>
/// Represents a collection of translations.
/// </summary>
/// <remarks>Translation keys are compared using <see cref="StringComparer.Ordinal"/>.</remarks>
public sealed class Translations : IEnumerable<KeyValuePair<string, string>>
{
#if NET9_0_OR_GREATER
    private readonly Lock mergeGate = new();
#else
    private readonly object mergeGate = new();
#endif

    private readonly Dictionary<string, string> store;

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
        get => Get(key);
        set => Set(key, value);
    }

    /// <summary>
    /// Tries to get the translation for the specified key.
    /// </summary>
    /// <param name="key">The key of the translation.</param>
    /// <param name="translation">The translation.</param>
    /// <returns>The true if found; otherwise false.</returns>
    public bool TryGet(string key, [NotNullWhen(true)] out string? translation)
    {
        return store.TryGetValue(key, out translation);
    }

    /// <summary>
    /// Gets the translation for the specified key.
    /// </summary>
    /// <param name="key">The key of the translation.</param>
    /// <returns>The translation if found; otherwise, the key.</returns>
    public string Get(string key)
    {
        return store.GetValueOrDefault(key, key);
    }

    /// <summary>
    /// Sets the translation for the specified key.
    /// </summary>
    /// <param name="key">The key of the translation.</param>
    /// <param name="translation">The translation.</param>
    public void Set(string key, string translation)
    {
        store[key] = translation;
    }

    /// <summary>
    /// Removes the translation for the specified key.
    /// </summary>
    /// <param name="key">The key of the translation.</param>
    public void Remove(string key)
    {
        store.Remove(key);
    }

    /// <summary>
    /// Merges the specified dictionary of values into the current translations.
    /// </summary>
    /// <param name="values">The dictionary of values to merge.</param>
    /// <remarks>Same keys will replace older values.</remarks>
    public Translations Merge(Translations translations)
    {
        lock (mergeGate)
        {
            foreach (var (k, v) in translations.store)
            {
                store[k] = v;
            }
            return this;
        }
    }

    /// <summary>
    /// Merges the specified dictionary of values into the current translations.
    /// </summary>
    /// <param name="values">The dictionary of values to merge.</param>
    /// <remarks>Same keys will replace older values.</remarks>
    public Translations Merge(IDictionary<string, string> values)
    {
        lock (mergeGate)
        {
            foreach (var (k, v) in values)
            {
                store[k] = v;
            }
            return this;
        }
    }

    /// <summary>
    /// Merges the specified dictionary of values into the current translations.
    /// </summary>
    /// <param name="values">The dictionary of values to merge.</param>
    /// <remarks>Same keys will replace older values.</remarks>
    public Translations Merge(IEnumerable<KeyValuePair<string, string>> values)
    {
        lock (mergeGate)
        {
            foreach (var (k, v) in values)
            {
                store[k] = v;
            }
            return this;
        }
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
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
