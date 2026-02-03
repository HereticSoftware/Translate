using Metaphrase.Abstract;
using System.Buffers;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Metaphrase.Defaults;

/// <summary>
/// A default parser for translating and interpolating strings with parameters.
/// </summary>
/// <remarks>Example: "This is a {key}" ==> "This is a value", with params = new { key = "value" }</remarks>
public sealed partial class DefaultTranslateParser : TranslateParser
{
    [GeneratedRegex(@"(?<!\{)\{(?<key>[a-zA-Z0-9][a-zA-Z0-9\-.]*)\}(?!\})")]
    private static partial Regex ParameterRegex();

    /// <summary>
    /// Gets the singleton instance of the <see cref="DefaultTranslateParser"/> class.
    /// </summary>
    public static DefaultTranslateParser Instance { get; } = new();

    /// <inheritdoc/>
    public override bool ShouldInterpolate(string expr)
    {
        return ParameterRegex().IsMatch(expr);
    }

    /// <inheritdoc/>
    public override string Interpolate(string expr, object? parameters)
    {
        if (parameters is null)
        {
            return expr;
        }

        var type = parameters.GetType();
        var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        if (props.Length == 0)
        {
            return expr;
        }

        using var buffer = new Buffer(parameters, props);
        return ParameterRegex().Replace(expr, match =>
        {
            var defaultValue = match.Value;
            var key = match.Groups[1].Value;
            var value = buffer.GetOrAdd(key);
            return value ?? defaultValue;
        });
    }

    private readonly struct Buffer : IDisposable
    {
        private readonly object parameters;
        private readonly PropertyInfo[] properties;
        private readonly KeyValuePair<string, string?>[] buffer;

        public Buffer(object parameters, PropertyInfo[] properties)
        {
            this.parameters = parameters;
            this.properties = properties;
            buffer = ArrayPool<KeyValuePair<string, string?>>.Shared.Rent(properties.Length);
        }

        public string? GetOrAdd(string key)
        {
            var l = buffer.Length;
            var length = properties.Length;
            var nextIndex = 0;
            for (int i = 0; i < length; i++)
            {
                if (buffer[i] is { Key: default(string), Value: default(string) })
                {
                    nextIndex = i;
                    break;
                }
                if (buffer[i].Key == key)
                {
                    return buffer[i].Value;
                }
            }
            for (int i = 0; i < length; i++)
            {
                var prop = properties[i];
                if (prop.Name.Equals(key, StringComparison.Ordinal))
                {
                    var value = prop.GetValue(parameters)?.ToString();
                    buffer[nextIndex] = KeyValuePair.Create(key, value);
                    return value;
                }
            }
            return null;
        }

        public void Dispose()
        {
            ArrayPool<KeyValuePair<string, string?>>.Shared.Return(buffer);
        }
    }
}
