namespace Metaphrase.Test.Unit;

file class TestParser : TranslateParser
{
    public object? Parameters { get; private set; }

    public override string Interpolate(string expr, object? parameters)
    {
        Parameters = parameters;
        return DefaultTranslateParser.Instance.Interpolate(expr, parameters);
    }
}

public sealed class TranslateStringTests
{
    [Test]
    public async Task Does_Not_Use_Parser()
    {
        var parser = new TestParser();
        var template = "Hello world!";
        var expected = "Hello world!";

        var str = new TranslateString(template, null, parser);
        var actual = str.ToString();

        await That(parser.Parameters).IsNull();
        await That(actual).IsEqualTo(expected);
    }

    [Test]
    public async Task Uses_Parser()
    {
        var parser = new TestParser();
        var template = "Hello {name}!";
        var expected = "Hello world!";

        var parameters = new { name = "world" };
        var str = new TranslateString(template, parameters, parser);
        var actual = str.ToString();

        await That(parser.Parameters).IsNotNull();
        await That(actual).IsEqualTo(expected);
    }
}
