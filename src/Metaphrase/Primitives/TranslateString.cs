using Metaphrase.Abstract;
using Metaphrase.Defaults;

namespace Metaphrase.Primitives;

/// <summary>
/// Represents a string that can be translated using specified parameters and a parser.
/// </summary>
public readonly ref struct TranslateString
{
    private readonly string value;
    private readonly TranslateParser parser;
    private readonly object? parameters;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslateString"/> struct.
    /// </summary>
    /// <param name="value">The string value to be translated.</param>
    /// <param name="parameters">The parameters used for translation.</param>
    /// <param name="parser">The parser used for translation. If null, the default parser is used.</param>
    public TranslateString(string value, object? parameters, TranslateParser? parser)
    {
        this.value = value;
        this.parameters = parameters;
        this.parser = parser ?? DefaultTranslateParser.Instance;
    }

    /// <summary>
    /// Defines a custom operator for the TranslateString struct.
    /// </summary>
    /// <param name="parameters">The parameters to be used for translation.</param>
    /// <returns>A new instance of <see cref="TranslateString"/> with the specified parameters.</returns>
    public static TranslateString operator |(TranslateString translateString, object? parameters)
    {
        return new(translateString.value, parameters, translateString.parser);
    }

    /// <summary>
    /// Returns the translated string.
    /// </summary>
    /// <returns>The translated string.</returns>
    public override string ToString()
    {
        return parser.Interpolate(value, parameters);
    }

    /// <summary>
    /// Implicitly converts a <see cref="TranslateString"/> to a <see cref="string"/>.
    /// </summary>
    /// <param name="str">The <see cref="TranslateString"/> to convert.</param>
    public static implicit operator string(TranslateString str)
    {
        return str.ToString();
    }
}
