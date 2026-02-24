using Metaphrase.SmartFormat;

namespace Metaphrase.Test.Unit;

public sealed class ParserTests
{
    public sealed record Input
    {
        public required string Case { get; init; }

        public required string Translation { get; init; }

        public required string Expected { get; set; }

        public required object? Parameters { get; init; }

        public override string ToString()
        {
            return Case;
        }

        public void Deconstruct(out string template, out string expected, out object? parameters)
        {
            template = Translation;
            expected = Expected;
            parameters = Parameters;
        }
    }

    public static Input[] Data() =>
    [
        new() {
            Case = "Handle One Parameter",
            Translation = "Welcome {user}!",
            Expected = "Welcome Alex!",
            Parameters = new { user = "Alex" }
        },
        new() {
            Case = "Handle Two Parameters One Available",
            Translation = "Good morning {user}, have a great {day}!",
            Expected = "Good morning Alex, have a great {day}!",
            Parameters = new { user = "Alex" }
        },
        new() {
            Case = "Handle One Parameter Empty Object",
            Translation = "Your order number {order} is confirmed.",
            Expected = "Your order number {order} is confirmed.",
            Parameters = new { }
        },
        new() {
            Case = "Handle Three Parameters",
            Translation = "Dear {name}, your appointment is on {date} at {time}.",
            Expected = "Dear John, your appointment is on 15th May at 10 AM.",
            Parameters = new { name = "John", date = "15th May", time = "10 AM" }
        },
        new() {
            Case = "Handle Null Value",
            Translation = "This is {key}",
            Expected = "This is ",
            Parameters = new { key = (string?)null }
        },
        new() {
            Case = "Handle Boolean Value",
            Translation = "This is a bool: {key}",
            Expected = "This is a bool: True",
            Parameters = new { key = true }
        },
        new() {
            Case = "Handle Numeric Value",
            Translation = "Count: {key}",
            Expected = "Count: 42",
            Parameters = new { key = 42 }
        },
        new() {
            Case = "Return Original When No Parameters",
            Translation = "This is a {key}",
            Expected = "This is a {key}",
            Parameters = null
        },
        new() {
            Case = "Return Original When No Match",
            Translation = "This has no placeholders",
            Expected = "This has no placeholders",
            Parameters = new { key = "value" }
        },
        new() {
            Case = "Handle Multiple Occurrences Of Same Key",
            Translation = "Say {word} and {word} again",
            Expected = "Say hello and hello again",
            Parameters = new { word = "hello" }
        },
        new() {
            Case = "Escape Double Braces",
            Translation = "This is {{not}} a placeholder",
            Expected = "This is {{not}} a placeholder",
            Parameters = new { not = "replaced" }
        },
    ];

    public static TranslateParser[] Parsers() =>
    [
        DefaultTranslateParser.Instance,
        SmartFormatParser.Instance,
    ];

    [Test]
    [MatrixDataSource]
    [DisplayName("$input |$parser")]
    public async Task Parse(
        [MatrixMethod<ParserTests>(nameof(Data))] Input input,
        [MatrixMethod<ParserTests>(nameof(Parsers))] TranslateParser parser)
    {
        var parserCallCount = 0;
        var parserWrapped = new FakeParser((expr, parameters) =>
        {
            Interlocked.Increment(ref parserCallCount);
            return parser.Interpolate(expr, parameters);
        });
        var cache = new FakeCache("en", "test", input.Translation);
        var service = new TranslateService(parser: parserWrapped, cache: cache, options: new() { Current = "en" });

        var instant = service.Instant("test", input.Parameters).ToString();
        var servicePipe = (service | "test" | input.Parameters).ToString();
        var pipeService = ("test" | service | input.Parameters).ToString();

        await That(instant).IsEqualTo(input.Expected);
        await That(servicePipe).IsEqualTo(input.Expected);
        await That(pipeService).IsEqualTo(input.Expected);

        await That(parserCallCount).IsEqualTo(3);
    }
}
