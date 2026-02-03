using Metaphrase.Primitives.Events;

namespace Metaphrase.Test.Unit;

public sealed class TranslationsTests
{
    [Test]
    public async Task Events_Should_Work()
    {
        TranslationChangeEvent? lastEvent = null;
        var translations = new Translations();
        translations["title"] = "A Project";
        translations["settings"] = "Settings";
        translations.OnTranslationChange.Subscribe(e => lastEvent = e);

        (string key, string value)[] changes =
        [
            ("title", "next"),
            ("settings", "next"),
            ("aaaa", "ooo"),
            ("noice.not", "yy"),
        ];

        foreach (var (k, v) in changes)
        {
            translations[k] = v;
            await Assert.That(k).IsEqualTo(lastEvent?.Key);
            await Assert.That(v).IsEqualTo(lastEvent?.Translation);
        }
    }
}
