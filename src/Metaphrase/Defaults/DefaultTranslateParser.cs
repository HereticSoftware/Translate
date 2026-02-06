using Metaphrase.Abstract;
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
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        if (properties.Length == 0)
        {
            return expr;
        }

        var result = ParameterRegex().Replace(expr, match =>
        {
            var key = match.Groups[1].Value;
            var length = properties.Length;
            foreach (var property in properties)
            {
                if (property.Name.Equals(key, StringComparison.Ordinal))
                {
                    return property.GetValue(parameters)?.ToString() ?? match.Value;
                }
            }
            return match.Value;
        });
        return result;
    }
}
