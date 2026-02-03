using Metaphrase.Abstract;
using SmartFormat;
using SmartFormat.Core.Settings;

namespace Metaphrase.SmartFormat;

/// <summary>
/// A parser for interpolating strings using SmartFormat.
/// </summary>
public sealed class SmartFormatParser : TranslateParser
{
    /// <summary>
    /// Gets the singleton instance of the <see cref="SmartFormatParser"/> class.
    /// </summary>
    public static SmartFormatParser Instance { get; } = new();

    private readonly SmartFormatter formatter;

    /// <summary>
    /// Initializes a new instance of the <see cref="SmartFormatParser"/> class with default settings.
    /// </summary>
    public SmartFormatParser() : this(settings: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SmartFormatParser"/> class with the specified formatter.
    /// </summary>
    /// <param name="formatter">The <see cref="SmartFormatter"/> to use for formatting.</param>
    public SmartFormatParser(SmartFormatter formatter)
    {
        this.formatter = new(formatter.Settings);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SmartFormatParser"/> class with the specified settings.
    /// </summary>
    /// <param name="settings">The <see cref="SmartSettings"/> to use for formatting.</param>
    public SmartFormatParser(SmartSettings? settings = null)
    {
        settings ??= new()
        {
            Parser = new() { ErrorAction = ParseErrorAction.MaintainTokens },
            Formatter = new() { ErrorAction = FormatErrorAction.MaintainTokens },
        };
        formatter = Smart.CreateDefaultSmartFormat(settings);
    }

    /// <inheritdoc/>
    public override bool ShouldInterpolate(string expr)
    {
        return expr.Contains('{') && expr.Contains('}');
    }

    /// <inheritdoc/>
    public override string Interpolate(string expr, object? parameters)
    {
        return parameters is null
            ? expr
            : formatter.Format(expr, [parameters]);
    }
}
