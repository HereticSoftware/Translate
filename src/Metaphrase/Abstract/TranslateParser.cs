namespace Metaphrase.Abstract;

/// <summary>
/// A parser for translating and interpolating strings.
/// </summary>
public abstract class TranslateParser
{
    /// <summary>
    /// Determines whether the given expression should be interpolated.
    /// </summary>
    /// <param name="expr">The expression string to evaluate.</param>
    /// <returns>True if the expression should be interpolated; otherwise, false.</returns>
    public abstract bool ShouldInterpolate(string expr);

    /// <summary>
    /// Interpolates a string to replace parameters.
    /// </summary>
    /// <param name="expr">The expression string containing placeholders.</param>
    /// <param name="parameters">An object containing the parameters to replace in the expression.</param>
    /// <returns>The interpolated string with parameters replaced.</returns>
    public abstract string Interpolate(string expr, object? parameters);
}
