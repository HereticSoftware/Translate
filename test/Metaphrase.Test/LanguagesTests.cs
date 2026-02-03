using Metaphrase.Primitives.Events;

namespace Metaphrase.Test.Unit;

public sealed class LanguagesTests
{
    [Test]
    public async Task Events_Should_Work()
    {
        LanguageTranslationChangeEvent? lastEvent = null;
        var languages = new Languages();
        languages.Get("en")["title"] = "A Project";
        languages.Get("el")["settings"] = "Settings";
        languages.OnTranslationChange.Subscribe(e => lastEvent = e);

        (string lang, string key, string value)[] changes =
        [
            ("en", "title", "next1"),
            ("en", "settings", "next2"),
            ("en", "aaaa", "ooo"),
            ("en", "noice.not", "yy"),
            ("el", "title", "next2"),
            ("el", "settings", "next"),
            ("el", "aaaa", "ooo4"),
            ("el", "noice.not", "yy"),
            ("es", "", "{}"),
        ];

        foreach (var (l, k, v) in changes)
        {
            languages.Get(l)[k] = v;
            await Assert.That(l).IsEqualTo(lastEvent?.Lang);
            await Assert.That(k).IsEqualTo(lastEvent?.Key);
            await Assert.That(v).IsEqualTo(lastEvent?.Translation);
        }
    }
}
