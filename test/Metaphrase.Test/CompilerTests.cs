namespace Metaphrase.Test;

public sealed class CompilerTests
{
    private readonly FakeLoader loader = new(new Translations
    {
        ["LOAD"] = "This is a test"
    });

    private readonly FakeCompiler compiler = new((language, translation) =>
    {
        return $"{translation}|compiled";
    });

    [Test]
    public async Task Default_Should_Be_Used_On_Loading()
    {
        var service = new TranslateService(loader: loader)
        {
            Current = "en"
        };

        await service.LoadCurrent();

        var translation = service.Instant("LOAD").ToString();

        await That(translation).IsEqualTo("This is a test");
    }

    [Test]
    public async Task Default_Should_Be_Used_On_Set_Translations()
    {
        var service = new TranslateService(loader: loader)
        {
            Current = "en"
        };

        await service.SetTranslations("en", new Translations
        {
            ["SET-TRANSLATION"] = "A manually added translation"
        });

        var translation = service.Instant("SET-TRANSLATION").ToString();

        await That(translation).IsEqualTo("A manually added translation");
    }

    [Test]
    public async Task Default_Should_Be_Used_On_Set_Single_Translation()
    {
        var service = new TranslateService(loader: loader)
        {
            Current = "en"
        };

        await service.Set("en", "SET", "Another manually added translation");

        var translation = service.Instant("SET").ToString();

        await That(translation).IsEqualTo("Another manually added translation");
    }

    [Test]
    public async Task Custom_Should_Be_Used_On_Loading()
    {
        var service = new TranslateService(loader: loader, compiler: compiler)
        {
            Current = "en"
        };

        await service.LoadCurrent();

        var translation = service.Instant("LOAD").ToString();

        await That(translation).IsEqualTo("This is a test|compiled");
    }

    [Test]
    public async Task Custom_Should_Be_Used_On_Set_Translations()
    {
        var service = new TranslateService(loader: loader, compiler: compiler)
        {
            Current = "en"
        };

        await service.SetTranslations("en", new Translations
        {
            ["SET-TRANSLATION"] = "A manually added translation"
        });

        var translation = service.Instant("SET-TRANSLATION").ToString();

        await That(translation).IsEqualTo("A manually added translation|compiled");
    }

    [Test]
    public async Task Custom_Should_Be_Used_On_Set_Single_Translation()
    {
        var service = new TranslateService(loader: loader, compiler: compiler)
        {
            Current = "en"
        };

        await service.Set("en", "SET", "Another manually added translation");

        var translation = service.Instant("SET").ToString();

        await That(translation).IsEqualTo("Another manually added translation|compiled");
    }
}
