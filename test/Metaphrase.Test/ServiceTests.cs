namespace Metaphrase.Test;

public sealed class ServiceTests
{
    [Test]
    public async Task Multiple_Instances_Defaults_That_Should_Be_Shared()
    {
        var service1 = new TranslateService();
        var service2 = new TranslateService();

        var get = (string field) => (service1.GetPrivateFieldValue(field), service2.GetPrivateFieldValue(field));

        (object?, object?)[] toCompare = [
            get("parser"),
            get("loader"),
            get("missingHandler"),
            get("compiler"),
        ];

        foreach (var (left, right) in toCompare)
        {
            await That(left).IsNotNull();
            await That(right).IsNotNull();
            await That(left).IsSameReferenceAs(right);
        }
    }

    [Test]
    public async Task Multiple_Instances_Defaults_That_Should_Be_Isolated()
    {
        var service1 = new TranslateService();
        var service2 = new TranslateService();

        var get = (string field) => (service1.GetPrivateFieldValue(field), service2.GetPrivateFieldValue(field));

        (object?, object?)[] toCompare = [
            get("cache"),
            get("options"),
        ];

        foreach (var (left, right) in toCompare)
        {
            await That(left).IsNotNull();
            await That(right).IsNotNull();
            await That(left).IsNotSameReferenceAs(right);
        }
    }

    [Test]
    public async Task Isolated_Services_Should_Maintain_Separate_Caches()
    {
        var cache1 = new DefaultTranslateCache();
        var service1 = new TranslateService(cache: cache1);

        var cache2 = new DefaultTranslateCache();
        var service2 = new TranslateService(cache: cache2);

        var get = (string field) => (service1.GetPrivateFieldValue(field), service2.GetPrivateFieldValue(field));
        var (left, right) = get("cache");

        await That(left).IsNotNull();
        await That(right).IsNotNull();
        await That(left).IsNotEqualTo(right);
    }

    [Test]
    public async Task Same_Cache_Should_Share_Translations()
    {
        var sharedCache = new DefaultTranslateCache();
        var service1 = new TranslateService(cache: sharedCache);
        var service2 = new TranslateService(cache: sharedCache);

        var translations1 = service1.InstantTranslations("en");
        var translations2 = service2.InstantTranslations("en");

        await That(translations1).IsEqualTo(translations2);
    }

    [Test]
    public async Task Same_Parser_Should_Work_Consistently_Across_Services()
    {
        var sharedCache = new FakeCache("en", "test", "Test {value}");
        var sharedParserCallCount = 0;
        var sharedParser = new FakeParser((expr, parameters) =>
        {
            Interlocked.Increment(ref sharedParserCallCount);
            return DefaultTranslateParser.Instance.Interpolate(expr, parameters);
        });
        var sharedOptions = new TranslateServiceOptions { Current = "en" };

        var service1 = new TranslateService(parser: sharedParser, cache: sharedCache, options: sharedOptions);
        var service2 = new TranslateService(parser: sharedParser, cache: sharedCache, options: sharedOptions);

        var str1 = (service1 | "test" | new { value = "1" }).ToString();
        var str2 = (service2 | "test" | new { value = "2" }).ToString();

        await That(str1).IsEqualTo("Test 1");
        await That(str2).IsEqualTo("Test 2");
        await That(sharedParserCallCount).IsEqualTo(2);
    }

    [Test]
    public async Task Different_Parser_Should_Behave_Differently()
    {
        var parser1CallCount = 0;
        var parser2CallCount = 0;
        var parser1 = new FakeParser((expr, parameters) =>
        {
            Interlocked.Increment(ref parser1CallCount);
            return DefaultTranslateParser.Instance.Interpolate(expr, parameters);
        });
        var parser2 = new FakeParser((expr, parameters) =>
        {
            Interlocked.Increment(ref parser2CallCount);
            return DefaultTranslateParser.Instance.Interpolate(expr, parameters);
        });

        var service1 = new TranslateService(parser: parser1);
        var service2 = new TranslateService(parser: parser2);

        service1.Current = service2.Current = "en";
        await Observable.Zip(
            service1.Set("en", "test", "Hello {name}"),
            service2.Set("en", "test", "Hello {name}")
        );

        var str1 = (service1 | "test" | "Hello {name}" | new { name = "Alice" }).ToString();
        var str2 = (service2 | "test" | "Hello {name}" | new { name = "Bob" }).ToString();

        await That(str1).IsEqualTo("Hello Alice");
        await That(str2).IsEqualTo("Hello Bob");

        await That(parser1CallCount).IsEqualTo(1);
        await That(parser2CallCount).IsEqualTo(1);
    }

    [Test]
    public async Task Different_Missing_Handlers_Should_Behave_Differently()
    {
        var handler1CallCount = 0;
        var handler2CallCount = 0;

        var handler1 = new FakeMissingHandler((l, k) =>
        {
            Interlocked.Increment(ref handler1CallCount);
            return k;
        });
        var handler2 = new FakeMissingHandler((l, k) =>
        {
            Interlocked.Increment(ref handler2CallCount);
            return k;
        });

        var service1 = new TranslateService(missingHandler: handler1);
        var service2 = new TranslateService(missingHandler: handler2);

        var value1 = await service1.Get("en", "missing_key");
        var value2 = await service2.Get("en", "missing_key");

        await That(handler1CallCount).IsEqualTo(1);
        await That(handler2CallCount).IsEqualTo(1);
    }

    [Test]
    public async Task Event_Listeners_Should_Be_Independent_Per_Service()
    {
        var service1 = new TranslateService();
        var service2 = new TranslateService();

        var changes1 = new List<string>();
        var changes2 = new List<string>();

        service1.OnCurrentChange.Subscribe(evt => changes1.Add(evt.Language));
        service2.OnCurrentChange.Subscribe(evt => changes2.Add(evt.Language));

        service1.Current = "en";
        service2.Current = "fr";

        await That(changes1.Count).IsEqualTo(1);
        await That(changes2.Count).IsEqualTo(1);
        await That(changes1[0]).IsEqualTo("en");
        await That(changes2[0]).IsEqualTo("fr");
    }

    [Test]
    public async Task Should_Respect_Configured_Options()
    {
        var options = new TranslateServiceOptions
        {
            Current = "en",
            Fallback = "fr"
        };

        var service = new TranslateService(options: options);

        await That(service.Current).IsEqualTo("en");
        await That(service.Fallback).IsEqualTo("fr");
    }

    [Test]
    public async Task Should_Initialize_With_Available_Languages()
    {
        var options = new TranslateServiceOptions
        {
            Available = ["en", "fr", "de"]
        };
        var service = new TranslateService(options: options);

        await That(service.Available.Count).IsEqualTo(3);
        await That(service.Available.Contains("en")).IsTrue();
        await That(service.Available.Contains("fr")).IsTrue();
        await That(service.Available.Contains("de")).IsTrue();
    }

    [Test]
    public async Task Load_Should_Cache_Translations_On_Repeated_Calls()
    {
        var loader = new FakeLoader();
        var service = new TranslateService(loader: loader);

        await service.Load("en");
        await loader.That(1);

        await service.Load("en");
        await loader.That(1);

        await service.Load("en");
        await loader.That(1);
    }

    [Test]
    public async Task Load_Should_Support_Multiple_Languages()
    {
        var loader = new FakeLoader();
        var service = new TranslateService(loader: loader);

        await service.Load("en");
        await service.Load("fr");
        await service.Load("de");

        await loader.That(3);
        await That(service.Available.Count).IsEqualTo(0); // Available is not auto-populated
    }

    [Test]
    public async Task Current_Language_Should_Change_On_LoadAndSetCurrent()
    {
        var loader = new FakeLoader();
        var service = new TranslateService(loader: loader);

        await That(service.Current).IsEqualTo("");

        await service.LoadAndSetCurrent("en");
        await That(service.Current).IsEqualTo("en");

        await service.LoadAndSetCurrent("fr");
        await That(service.Current).IsEqualTo("fr");

        await loader.That(2);
    }

    [Test]
    public async Task Fallback_Language_Should_Change_On_LoadAndSetFallback()
    {
        var loader = new FakeLoader();
        var service = new TranslateService(loader: loader);

        await That(service.Fallback).IsEqualTo("");

        await service.LoadAndSetFallback("en");
        await That(service.Fallback).IsEqualTo("en");

        await service.LoadAndSetFallback("fr");
        await That(service.Fallback).IsEqualTo("fr");

        await loader.That(2);
    }

    [Test]
    public async Task Should_Not_Reload_When_Already_Loaded()
    {
        var loader = new FakeLoader();
        var service = new TranslateService(loader: loader);

        await service.LoadAndSetCurrent("en");
        await loader.That(1);

        await service.LoadAndSetCurrent("en");
        await loader.That(1);

        await service.LoadCurrent();
        await loader.That(1);
    }

    [Test]
    public async Task Reload_Should_Refresh_Cached_Language()
    {
        var loader = new FakeLoader();
        var service = new TranslateService(loader: loader);

        await service.Load("en");
        await loader.That(1);

        await service.Reload("en");
        await loader.That(2);
    }

    [Test]
    public async Task Should_Emit_OnCurrentChange_When_Language_Changes()
    {
        var loader = new FakeLoader();
        var service = new TranslateService(loader: loader);
        var languageChanges = new List<string>();

        service.OnCurrentChange.Subscribe(evt => languageChanges.Add(evt.Language));

        await service.LoadAndSetCurrent("en");
        await service.LoadAndSetCurrent("en");
        await service.LoadAndSetCurrent("fr");

        await That(languageChanges.Count).IsEqualTo(2);
        await That(languageChanges[0]).IsEqualTo("en");
        await That(languageChanges[1]).IsEqualTo("fr");
        await loader.That(2);
    }

    [Test]
    public async Task Should_Emit_OnFallbackLangChange_When_Fallback_Changes()
    {
        var loader = new FakeLoader();
        var service = new TranslateService(loader: loader);
        var fallbackChanges = new List<string>();

        service.OnFallbackLangChange.Subscribe(evt => fallbackChanges.Add(evt.Language));

        await service.LoadAndSetFallback("en");
        await service.LoadAndSetFallback("en");
        await service.LoadAndSetFallback("fr");

        await That(fallbackChanges.Count).IsEqualTo(2);
        await That(fallbackChanges[0]).IsEqualTo("en");
        await That(fallbackChanges[1]).IsEqualTo("fr");
        await loader.That(2);
    }

    [Test]
    public async Task Should_Handle_Empty_Language_String()
    {
        var loader = new FakeLoader();
        var service = new TranslateService(loader: loader);

        var result = await service.Load("").FirstOrDefaultAsync();
        await That(result).IsNull();
        await loader.That(0);
    }

    [Test]
    public async Task Should_Handle_Null_Language_String()
    {
        var loader = new FakeLoader();
        var service = new TranslateService(loader: loader);

        var result = await service.Load(null!).FirstOrDefaultAsync();
        await That(result).IsNull();
        await loader.That(0);
    }

    [Test]
    public async Task Reset_Should_Allow_Reloading_Language()
    {
        var loader = new FakeLoader();
        var service = new TranslateService(loader: loader);

        await service.Load("en");
        await loader.That(1);

        await service.Reset("en");

        await service.Load("en");
        await loader.That(2);
    }

    [Test]
    public async Task Should_Maintain_Available_Languages()
    {
        var loader = new FakeLoader();
        var service = new TranslateService(loader: loader);

        service.AddAvailable("en", "fr", "de");
        await That(service.Available.Count).IsEqualTo(3);

        service.AddAvailable("es");
        await That(service.Available.Count).IsEqualTo(4);
    }

    [Test]
    public async Task Instant_Should_Return_Key_When_Missing()
    {
        var service = new TranslateService();

        var translation = service.Instant("en", "missing_key", null).ToString();
        await That(translation).IsEqualTo("missing_key");
    }

    [Test]
    public async Task Instant_Should_Return_Value_When_Available()
    {
        var cache = new FakeCache("en", "missing_key", "found");
        var service = new TranslateService(cache: cache);

        var translation = service.Instant("en", "missing_key", null).ToString();
        await That(translation).IsEqualTo("found");
    }

    [Test]
    public async Task Should_Fallback_To_Fallback_Language()
    {
        var options = new TranslateServiceOptions { Current = "en", Fallback = "fr" };
        var cache = new FakeCache([
            ("en", []),
            ("fr", [("missing_key", "found in fr")])
        ]);
        var service = new TranslateService(cache: cache, options: options);

        var translation = service.Instant("missing_key").ToString();
        await That(translation).IsEqualTo("found in fr");
    }

    [Test]
    [Timeout(5_000)]
    public async Task Should_Handle_Sequential_Language_Loads(CancellationToken cancellationToken)
    {
        var loader = new FakeLoader(TimeSpan.FromMilliseconds(10));
        var service = new TranslateService(loader: loader);

        var en = service.Load("en");
        var fr = service.Load("fr");
        var de = service.Load("de");

        await Observable.Zip(en, fr, de).ToTask(cancellationToken);

        await loader.That(3);
    }

    [Test]
    public async Task Should_Reload_Specific_Language()
    {
        var loader = new FakeLoader(TimeSpan.FromMilliseconds(50));
        var service = new TranslateService(loader: loader);

        await service.Load("en");
        await service.Load("fr");
        await loader.That(2);

        await service.Reload("en");
        await loader.That(3);
    }

    [Test]
    [Timeout(10_000)]
    public async Task Should_Handle_Concurrent_Resets(CancellationToken cancellationToken)
    {
        var loader = new FakeLoader(TimeSpan.FromMilliseconds(50));
        var service = new TranslateService(loader: loader);

        service.Load("en").Subscribe();

        await Observable
            .Range(0, 5, cancellationToken)
            .Select(_ => service.Reset("en"))
            .SelectMany(x => x)
            .ToArrayAsync(cancellationToken);
    }

    [Test]
    public async Task Reset_While_Loading_Resets()
    {
        var loader = new FakeLoader(TimeSpan.FromMilliseconds(100));
        var service = new TranslateService(loader: loader);

        service.LoadAndSetCurrent("en").Subscribe();
        await service.Reset("en");

        await loader.That(callCount: 1, returnCount: 0);

        await service.LoadAndSetCurrent("en");

        await loader.That(callCount: 2, returnCount: 1);

        await service.LoadAndSetCurrent("en");
        await service.LoadAndSetCurrent("en");

        await loader.That(callCount: 2, returnCount: 1);

        await That(service.Available.Count).IsEqualTo(0);
    }
}
