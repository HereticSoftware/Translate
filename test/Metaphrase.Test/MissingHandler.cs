namespace Metaphrase.Test;

public sealed class MissingHandlerTests
{
    private readonly FakeLoader loader = new(lang =>
    {
        return Observable.Return<Translations>(lang == "fake"
            ? new() { ["NOT_USED"] = "not used" }
            : new() { ["TEST"] = "This is a test" }
        );
    });

    [Test]
    public async Task Should_Skip_Missing_Handler_If_Its_The_Default()
    {
        var compilerCallCount = 0;
        var compiler = new FakeCompiler((language, translation) =>
        {
            Interlocked.Increment(ref compilerCallCount);
            return "comppiled";
        });
        var service = new TranslateService(loader: loader, compiler: compiler) { Current = "en" };

        var result = await service.Get("nonExistingKey");

        await That(compilerCallCount).IsEqualTo(0);
        await That(result).IsEqualTo("nonExistingKey");
    }

    [Test]
    public async Task Should_Use_The_MissingTranslationHandler_When_The_Key_Does_Not_Exist()
    {
        var handlerCallCount = 0;
        var handler = new FakeMissingHandler((language, key) =>
        {
            Interlocked.Increment(ref handlerCallCount);
            return Observable.Return("handled");
        });
        var compilerCallCount = 0;
        var compiler = new FakeCompiler((language, translation) =>
        {
            Interlocked.Increment(ref compilerCallCount);
            return translation;
        });
        var service = new TranslateService(loader: loader, missingHandler: handler, compiler: compiler) { Current = "en" };

        var result = await service.Get("nonExistingKey");

        await That(handlerCallCount).IsEqualTo(1);
        await That(compilerCallCount).IsEqualTo(1);
        await That(result).IsEqualTo("handled");
    }

    [Test]
    [Arguments("")]
    [Arguments(null)]
    [Arguments("nonExistingKey")]
    public async Task Should_Return_The_Key_When_Using_MissingTranslationHandler_And_The_Handler_Returns_Empty_Values(string? handlerResult)
    {
        var handlerCallCount = 0;
        var handler = new FakeMissingHandler((language, key) =>
        {
            Interlocked.Increment(ref handlerCallCount);
            return Observable.Return(handlerResult!);
        });
        var service = new TranslateService(loader: loader, missingHandler: handler) { Current = "en" };

        var result = await service.Get("nonExistingKey");

        await That(handlerCallCount).IsEqualTo(1);
        await That(result).IsEqualTo("nonExistingKey");
    }

    [Test]
    public async Task Should_Not_Call_The_MissingTranslationHandler_When_The_Key_Exists()
    {
        var handlerCallCount = 0;
        var handler = new FakeMissingHandler((language, key) =>
        {
            Interlocked.Increment(ref handlerCallCount);
            return Observable.Return("handled");
        });
        var service = new TranslateService(loader: loader, missingHandler: handler);

        await service.LoadAndSetCurrent("en");

        var result = await service.Get("TEST");

        await That(handlerCallCount).IsEqualTo(0);
        await That(result).IsEqualTo("This is a test");
    }

    [Test]
    public async Task Should_Not_Call_MissingTranslationHandler_When_We_Use_Instant()
    {
        var handlerCallCount = 0;
        var handler = new FakeMissingHandler((language, key) =>
        {
            Interlocked.Increment(ref handlerCallCount);
            return Observable.Return("handled");
        });
        var service = new TranslateService(loader: loader, missingHandler: handler) { Current = "en" };

        var result = service.Instant("nonExistingKey").ToString();

        await That(handlerCallCount).IsEqualTo(0);
        await That(result).IsEqualTo("nonExistingKey");
    }

    [Test]
    public async Task Should_Return_Translation_From_The_Missing_Translation_Handler()
    {
        var handlerCallCount = 0;
        var handler = new FakeMissingHandler((language, key) =>
        {
            Interlocked.Increment(ref handlerCallCount);
            return Observable.Return("handled");
        });
        var service = new TranslateService(loader: loader, missingHandler: handler);

        await service.LoadAndSetCurrent("en");
        await service.LoadAndSetCurrent("fake");

        var result = await service.Get("TEST");

        await That(handlerCallCount).IsEqualTo(1);
        await That(result).IsEqualTo("handled");
    }

    [Test]
    public async Task Should_Return_Translation_From_Cache()
    {
        var handlerCallCount = 0;
        var handler = new FakeMissingHandler((language, key) =>
        {
            Interlocked.Increment(ref handlerCallCount);
            return Observable.Return("handled");
        });
        var service = new TranslateService(loader: loader, missingHandler: handler);

        await service.LoadAndSetCurrent("fake");
        await service.LoadAndSetCurrent("en");

        var result = await service.Get("TEST");

        await That(handlerCallCount).IsEqualTo(0);
        await That(result).IsEqualTo("This is a test");
    }
}
