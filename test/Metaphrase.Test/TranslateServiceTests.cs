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
        service.ResetLang("en");

        await Assert.That(loader.CallCount).IsEqualTo(1);
        await Assert.That(loader.ReturnCount).IsEqualTo(0);

        service.SetCurrentLang("en").Subscribe(); // Call 2 and 1 return
        await Task.Delay(TimeSpan.FromMilliseconds(200));

        await Assert.That(loader.CallCount).IsEqualTo(2);
        await Assert.That(loader.ReturnCount).IsEqualTo(1);

        await service.SetCurrentLang("en").FirstAsync(); // Call 2 and 1 return
        await service.SetCurrentLang("en").FirstAsync(); // Call 2 and 1 return

        await Assert.That(loader.CallCount).IsEqualTo(2);
        await Assert.That(loader.ReturnCount).IsEqualTo(1);

        await Assert.That(service.Langs.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Load_Should_Be_Thread_Safe()
    {
        var loader = new CustomLoader(TimeSpan.FromMilliseconds(1));
        var service = new TranslateService(loader: loader);
        var iterations = Enumerable.Range(1, 100);

        await Parallel.ForEachAsync(iterations, async (i, ct) =>
        {
            await service.LoadTranslation("en").FirstAsync(cancellationToken: ct);
        });

        await Assert.That(loader.CallCount).IsEqualTo(1);
        await Assert.That(loader.ReturnCount).IsEqualTo(1);
    }
}
