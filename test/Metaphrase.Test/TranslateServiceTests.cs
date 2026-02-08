namespace Metaphrase.Test;

file sealed class CustomLoader : TranslateLoader
{
    private readonly TimeSpan valueDelay;
    private int callCount;
    private int returnCount;

    public int CallCount => callCount;

    public int ReturnCount => returnCount;

    public CustomLoader(TimeSpan valueDelay)
    {
        this.valueDelay = valueDelay;
    }

    public override Observable<Translations> GetTranslation(string lang)
    {
        Interlocked.Increment(ref callCount);

        return Observable
            .Timer(valueDelay)
            .Select(_ =>
            {
                Interlocked.Increment(ref returnCount);
                return new Translations();
            });
    }
}

public sealed class TranslateServiceTests
{
    [Test]
    public async Task Reset_Should_Work()
    {
        var loader = new CustomLoader(TimeSpan.FromMilliseconds(100));
        var service = new TranslateService(loader: loader);

        service.SetCurrentLang("en").Subscribe(); // Call 1 and 0 return
        service.Reset("en");

        await That(loader.CallCount).IsEqualTo(1);
        await That(loader.ReturnCount).IsEqualTo(0);

        service.SetCurrentLang("en").Subscribe(); // Call 2 and 1 return
        await Task.Delay(TimeSpan.FromMilliseconds(200));

        await That(loader.CallCount).IsEqualTo(2);
        await That(loader.ReturnCount).IsEqualTo(1);

        await service.SetCurrentLang("en").FirstAsync(); // Call 2 and 1 return
        await service.SetCurrentLang("en").FirstAsync(); // Call 2 and 1 return

        await That(loader.CallCount).IsEqualTo(2);
        await That(loader.ReturnCount).IsEqualTo(1);

        await That(service.Available.Count).IsEqualTo(1);
    }

    [Test]
    [Timeout(10_000)]
    public async Task Load_Should_Be_Thread_Safe(CancellationToken cancellationToken)
    {
        var loader = new CustomLoader(TimeSpan.FromMilliseconds(100));
        var service = new TranslateService(loader: loader);
        var iterations = Enumerable.Range(1, 100);

        await Parallel.ForEachAsync(iterations, cancellationToken, body: async (i, ct) =>
        {
            await service
                .LoadTranslation("en")
                .FirstAsync(ct)
                .WaitAsync(TimeSpan.FromSeconds(5), ct);
        });

        await That(loader.CallCount).IsEqualTo(1);
        await That(loader.ReturnCount).IsEqualTo(1);
    }
}
