using Metaphrase.SmartFormat;

namespace Metaphrase.Test.Unit;

public sealed class TranslateParserTests
{
    public sealed record Input
    {
        public required string Template { get; init; }

        public required string Expected { get; set; }

        public required object? Parameters { get; init; }

        public override string ToString()
        {
            return Template;
        }

        public void Deconstruct(out string template, out string expected, out object? parameters)
        {
            template = Template;
            expected = Expected;
            parameters = Parameters;
        }
    }

    public static Input[] Data() =>
    [
        new() {
            Template = "Welcome {user}!",
            Expected = "Welcome Alex!",
            Parameters = new { user = "Alex" }
        },
        new() {
            Template = "Good morning {user}, have a great {day}!",
            Expected = "Good morning Alex, have a great {day}!",
            Parameters = new { user = "Alex" }
        },
        new() {
            Template = "Your order number {order} is confirmed.",
            Expected = "Your order number {order} is confirmed.",
            Parameters = new { }
        },
        new() {
            Template = "Dear {name}, your appointment is on {date} at {time}.",
            Expected = "Dear John, your appointment is on 15th May at 10 AM.",
            Parameters = new { name = "John", date = "15th May", time = "10 AM" }
        },
        new() {
            Template = "From {sender} to {recipient}, message: {message}",
            Expected = "From {sender} to Alex, message: Hello!",
            Parameters = new { recipient = "Alex", message = "Hello!" }
        },
        new() {
            Template = "Event: {event} | Location: {location} | Date: {date} | Time: {time}",
            Expected = "Event: Conference | Location: NY | Date: {date} | Time: {time}",
            Parameters = new { @event = "Conference", location = "NY" }
        },
        new () {
            Template = "Status: {status}, Progress: {progress}%",
            Expected = "Status: Completed, Progress: 100%",
            Parameters = new { status = "Completed", progress = 100 }
        },
        new()
        {
            Template = "Hello {firstName} {lastName}, your ID is {id}.",
            Expected = "Hello John Doe, your ID is 12345.",
            Parameters = new { firstName = "John", lastName = "Doe", id = 12345 }
        },
        new()
        {
            Template = "Your balance is {balance} {currency}.",
            Expected = "Your balance is {balance} {currency}.",
            Parameters = null
        },
        new()
        {
            Template = "Product: {product}, Price: {price}, Discount: {discount}, Final Price: {finalPrice}",
            Expected = "Product: Laptop, Price: $1000, Discount: 10%, Final Price: $900",
            Parameters = new { product = "Laptop", price = "$1000", discount = "10%", finalPrice = "$900" }
        },
    ];

    public static TranslateParser[] Parsers() =>
    [
        DefaultTranslateParser.Instance,
        SmartFormatParser.Instance,
    ];

    [Test]
    [MatrixDataSource]
    [DisplayName("$parser $input")]
    public async Task Should_Replace(
        [MatrixMethod<TranslateParserTests>(nameof(Data))] Input input,
        [MatrixMethod<TranslateParserTests>(nameof(Parsers))] TranslateParser parser)
    {
        var (template, expected, parameters) = input;

        var actual = new TranslateString(template, parameters, parser).ToString();
        await Assert.That(actual).IsEqualTo(expected);

        actual = (new TranslateString(template, null, parser) | parameters).ToString();
        await Assert.That(actual).IsEqualTo(expected);
    }
}
