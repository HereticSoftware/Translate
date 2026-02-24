namespace Metaphrase.Test;

public sealed class LoaderTests
{
    private readonly FakeLoader loader = new(new Translations
    {
        ["TEST"] = "This is also a test"
    });

    private readonly FakeCompiler compiler = new((language, translation) =>
    {
        return $"{translation}|compiled";
    });

    [Test]
    public async Task Should_Use_Default()
    {
        var service = new TranslateService();

        service.LoadAndSetCurrent("en").Subscribe();

        var result = await service.Get("TEST");

        await That(result).IsEqualTo("TEST");
    }

    [Test]
    public async Task Should_Use_Custom()
    {
        var service = new TranslateService(loader: loader);

        service.LoadAndSetCurrent("en").Subscribe();

        var result = await service.Get("TEST");

        await That(result).IsEqualTo("This is also a test");
    }

    [Test]
    public async Task Should_Wait_For_Loader_Result()
    {
        var loader = new FakeLoader(TimeSpan.FromMilliseconds(100), new Translations { ["TEST"] = "This is a test" });
        var service = new TranslateService(loader: loader);

        service.LoadAndSetCurrent("en").Subscribe();

        var result = await service.Get("TEST");

        await That(result).IsEqualTo("This is a test");
    }

    [Test]
    [Timeout(10_000)]
    public async Task Should_Be_Thread_Safe(CancellationToken cancellationToken)
    {
        var loader = new FakeLoader(TimeSpan.FromMilliseconds(2));
        var service = new TranslateService(loader: loader);
        var iterations = Enumerable.Range(1, 100);

        await Parallel.ForEachAsync(iterations, cancellationToken, body: async (i, ct) =>
        {
            await service
                .Load("en")
                .FirstAsync(ct)
                .WaitAsync(TimeSpan.FromSeconds(5), ct);
        });

        await loader.That(1);
    }
}
